using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TimeManagementBot;

public class BotController(TaskManager taskManager)
{
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.Message && update.Message!.Type == MessageType.Text)
        {
            var chatId = update.Message.Chat.Id;
            var text = update.Message.Text;
            if (text is null)
            {
                await botClient.SendMessage(chatId, "К сожалению, я умею принимать только текстовые сообщения :(",
                    cancellationToken: cancellationToken);
                return;
            }

            // Получаем или создаем TaskRepository для данного чата
            if (text.StartsWith("/start", StringComparison.CurrentCultureIgnoreCase))
            {
                await botClient.SendMessage(
                    chatId,
                    "Привет! Я твой простой менеджер задач на день. Просто нажми кнопку \"добавить задачи\" отправь мне список задач (каждая задача на новой строке), чтобы начать. Чтобы посмотреть доступные действия - нажми кнопку \"Действия\"",
                    cancellationToken: cancellationToken);
                await SendAvailableActions(botClient, chatId, cancellationToken);
                return;
            }

            if (text.Equals("действия", StringComparison.CurrentCultureIgnoreCase))
            {
                await SendAvailableActions(botClient, chatId, cancellationToken);
                return;
            }

            if (text.Equals("задачи", StringComparison.CurrentCultureIgnoreCase))
            {
                await SendTaskList(botClient, chatId, cancellationToken);
                return;
            }

            if (text.Equals("завершить день", StringComparison.CurrentCultureIgnoreCase))
            {
                await SendDaySummary(botClient, chatId, cancellationToken);
                return;
            }

            if (text.Equals("отмена", StringComparison.CurrentCultureIgnoreCase))
            {
                await botClient.SendMessage(
                    chatId,
                    "Вы отменили выполнение текущей задачи.",
                    cancellationToken: cancellationToken);
                await SendAvailableActions(botClient, chatId, cancellationToken);
                return;
            }

            if (text.Equals("выполнено", StringComparison.CurrentCultureIgnoreCase))
            {
                var currentTask = taskManager.GetCurrentTask(chatId);
                if (currentTask is null)
                    throw new InvalidOperationException("Current task is null");
                
                taskManager.CompleteTask(chatId, currentTask.Id);
                taskManager.ResetCurrentTask(chatId);
                await botClient.SendMessage(
                    chatId,
                    "Отлично! Задача выполнена.",
                    cancellationToken: cancellationToken);
                await SendAvailableActions(botClient, chatId, cancellationToken);
                return;
            }

            if (text.Equals("удалить задачу", StringComparison.CurrentCultureIgnoreCase))
            {
                var currentTask = taskManager.GetCurrentTask(chatId);
                if (currentTask is null)
                    throw new InvalidOperationException("Current task is null");
                taskManager.ResetCurrentTask(chatId);
                taskManager.DeleteTask(chatId, currentTask.Id);

                await botClient.SendMessage(chatId, $"Задача \"{currentTask.Description}\" успешно удалена.",
                    cancellationToken: cancellationToken);
                await SendAvailableActions(botClient, chatId, cancellationToken);
                return;
            }

            // Add tasks
            var newTasks = text.Split(["\r\n", "\r", "\n"], StringSplitOptions.None)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToList();

            if (newTasks.Count != 0)
            {
                foreach (var taskDescription in newTasks)
                {
                    taskManager.AddTask(chatId, taskDescription);
                }

                await botClient.SendMessage(
                    chatId,
                    $"Добавлено {newTasks.Count} задач. Для просмотра списка задач нажми кнопку \"Задачи\"",
                    cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendMessage(
                    chatId,
                    "Не понял команду. Отправь список задач или нажми кнопку \"Действия\"",
                    cancellationToken: cancellationToken);
            }
        }
        else if (update is { Type: UpdateType.CallbackQuery, CallbackQuery: not null })
        {
            var callbackQuery = update.CallbackQuery;
            if (!int.TryParse(callbackQuery.Data, out var taskId))
                return;
            var chatId = callbackQuery.Message!.Chat.Id;

            var task = taskManager.GetTaskById(chatId, taskId);

            if (task is not null && !task.IsCompleted)
            {
                taskManager.SetCurrentTask(chatId, task.Id);
                await botClient.SendMessage(
                    chatId,
                    $"Вы выбрали задачу: {task.Description}.  Когда закончите, нажмите кнопку \"Выполнено\"",
                    replyMarkup: GetTaskActionKeyboard(),
                    cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendMessage(
                    chatId,
                    "Задача уже выполнена или не найдена.",
                    cancellationToken: cancellationToken);
            }
        }
    }

    public static Task HandleErrorAsync(ITelegramBotClient _, Exception exception,
        CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]" +
                                                       $"{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(errorMessage);
        return Task.CompletedTask;
    }

    private static ReplyMarkup GetTaskActionKeyboard()
    {
        return new ReplyKeyboardMarkup([
            ["Выполнено"],
            ["Назад"],
            ["Удалить задачу"]
        ])
        {
            ResizeKeyboard = true
        };
    }

    private static async Task SendAvailableActions(ITelegramBotClient botClient, long chatId,
        CancellationToken cancellationToken)
    {
        await botClient.SendMessage(
            chatId,
            "Доступные действия:",
            replyMarkup: new ReplyKeyboardMarkup([
                ["Задачи"],
                ["Завершить день"]
            ])
            {
                ResizeKeyboard = true
            },
            cancellationToken: cancellationToken);
    }

    private async Task SendTaskList(ITelegramBotClient botClient, long chatId,
        CancellationToken cancellationToken)
    {
        var incompleteTasks = taskManager.GetIncompleteTasks(chatId);
        if (incompleteTasks.Count == 0)
        {
            await botClient.SendMessage(
                chatId,
                "Список задач пуст. Отправьте мне список задач, чтобы начать.",
                cancellationToken: cancellationToken);
            return;
        }

        var inlineKeyboard = new InlineKeyboardMarkup(incompleteTasks.Select(task =>
            new List<InlineKeyboardButton>
            {
                new(task.Description)
                {
                    CallbackData = task.Id.ToString()
                }
            }));

        await botClient.SendMessage(
            chatId,
            "Список задач на сегодня. Нажми на задачу, чтобы взять ее в работу:",
            replyMarkup: inlineKeyboard,
            cancellationToken: cancellationToken);
    }

    private async Task SendDaySummary(ITelegramBotClient botClient, long chatId,
        CancellationToken cancellationToken)
    {
        var summary = taskManager.GetDaySummary(chatId);
        taskManager.ResetCompletedTasks(chatId);

        await botClient.SendMessage(
            chatId,
            summary,
            cancellationToken: cancellationToken);
    }
}
