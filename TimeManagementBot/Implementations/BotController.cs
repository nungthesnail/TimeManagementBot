using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TimeManagementBot.Interfaces;
using TimeManagementBot.Models;

namespace TimeManagementBot.Implementations;

public class BotController(ITaskManager taskManager, IUserStateManager userStateManager, ISummaryCreator summaryCreator,
    ICurrentTaskManager currentTaskManager, IResourceManager resourceManager) : IBotController
{
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken stoppingToken)
    {
        if (update.Type == UpdateType.Message && update.Message!.Type == MessageType.Text)
        {
            var chatId = update.Message.Chat.Id;
            var text = update.Message.Text;
            if (text is null)
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: resourceManager.GetTextResource(TextRes.OnlyTextAllowed),
                    cancellationToken: stoppingToken);
                return;
            }
            
            var userState = userStateManager.GetUserState(chatId);

            if (text.StartsWith("/start", StringComparison.CurrentCultureIgnoreCase))
            {
                await ProcessHelloMessageAsync(botClient, chatId, userState, stoppingToken);
                return;
            }

            switch (userState)
            {
                case UserState.None:
                    await ProcessNoneStateAsync(botClient, chatId, text, stoppingToken);
                    break;
                case UserState.DoingTask:
                    await ProcessDoingTaskStateAsync(botClient, chatId, text, stoppingToken);
                    break;
                case UserState.EnteringTasks:
                    await ProcessEnteringTasksStateAsync(botClient, chatId, text, stoppingToken);
                    break;
                default:
                    await ProcessErrorAsync(botClient, chatId, stoppingToken);
                    break;
            }
        }
        else if (update is { Type: UpdateType.CallbackQuery, CallbackQuery: not null })
        {
            await ProcessSelectingTaskAsync(botClient, update, stoppingToken);
        }
    }

    private async Task ProcessHelloMessageAsync(ITelegramBotClient botClient, long chatId, UserState userState,
        CancellationToken cancellationToken)
    {
        await botClient.SendMessage(
            chatId: chatId,
            text: resourceManager.GetTextResource(TextRes.Hello),
            cancellationToken: cancellationToken);
        if (userState == UserState.None)
            await SendAvailableActions(botClient, chatId, cancellationToken);
    }
    
    private async Task ProcessNoneStateAsync(ITelegramBotClient botClient, long chatId, string text,
        CancellationToken cancellationToken)
    {
        // Добавить задачи
        if (text.Equals(resourceManager.GetTextResource(TextRes.ActionAddTasks),
            StringComparison.CurrentCultureIgnoreCase))
        {
            userStateManager.SetUserState(chatId, UserState.EnteringTasks);
            await botClient.SendMessage(
                chatId: chatId,
                text: resourceManager.GetTextResource(TextRes.EnterTasks),
                cancellationToken: cancellationToken);
        }
        
        // Посмотреть задачи
        else if (text.Equals(resourceManager.GetTextResource(TextRes.ActionViewTasks),
                 StringComparison.CurrentCultureIgnoreCase))
        {
            await SendTaskList(botClient, chatId, cancellationToken);
        }

        // Завершить день
        else if (text.Equals(resourceManager.GetTextResource(TextRes.ActionFinishDay),
                 StringComparison.CurrentCultureIgnoreCase))
        {
            await SendDaySummary(botClient, chatId, cancellationToken);
        }

        else
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: resourceManager.GetTextResource(TextRes.UnknownCommand),
                cancellationToken: cancellationToken);
        }
    }
    
    private async Task SendDaySummary(ITelegramBotClient botClient, long chatId,
        CancellationToken cancellationToken)
    {
        var summary = await summaryCreator.GetDaySummaryAsync(chatId);
        await taskManager.ResetCompletedTasksAsync(chatId);

        await botClient.SendMessage(
            chatId: chatId,
            text: summary,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }
    
    private async Task SendTaskList(ITelegramBotClient botClient, long chatId,
        CancellationToken cancellationToken)
    {
        var incompleteTasks = await taskManager.GetIncompleteTasksAsync(chatId);
        if (incompleteTasks.Count == 0)
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: resourceManager.GetTextResource(TextRes.NoTasks),
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
            chatId: chatId,
            text: resourceManager.GetTextResource(TextRes.ListOfTasks),
            replyMarkup: inlineKeyboard,
            cancellationToken: cancellationToken);
    }

    private async Task ProcessDoingTaskStateAsync(ITelegramBotClient botClient, long chatId, string text,
        CancellationToken cancellationToken)
    {
        // Назад
        if (text.Equals(resourceManager.GetTextResource(TextRes.ActionReturn),
            StringComparison.CurrentCultureIgnoreCase))
        {
            currentTaskManager.ResetCurrentTask(chatId);
            await botClient.SendMessage(
                chatId: chatId,
                text: resourceManager.GetTextResource(TextRes.TaskCanceled),
                cancellationToken: cancellationToken);
            await SendAvailableActions(botClient, chatId, cancellationToken);
        }

        // Завершить задачу
        else if (text.Equals(resourceManager.GetTextResource(TextRes.ActionCompleteTask),
                 StringComparison.CurrentCultureIgnoreCase))
        {
            var currentTask = currentTaskManager.GetCurrentTask(chatId);
            if (currentTask is null)
                throw new InvalidOperationException("Current task is null");

            await taskManager.CompleteTaskAsync(chatId, currentTask.Id);
            currentTaskManager.ResetCurrentTask(chatId);
            await botClient.SendMessage(
                chatId: chatId,
                text: resourceManager.GetTextResource(TextRes.TaskDone),
                cancellationToken: cancellationToken);
            await SendAvailableActions(botClient, chatId, cancellationToken);
        }

        // Удалить задачу
        else if (text.Equals(resourceManager.GetTextResource(TextRes.ActionDeleteTask),
                 StringComparison.CurrentCultureIgnoreCase))
        {
            var currentTask = currentTaskManager.GetCurrentTask(chatId);
            if (currentTask is null)
                throw new InvalidOperationException("Current task is null");
            currentTaskManager.ResetCurrentTask(chatId);
            await taskManager.DeleteTaskAsync(chatId, currentTask.Id);

            await botClient.SendMessage(
                chatId: chatId,
                text: resourceManager.GetTextResource(TextRes.TaskDeleted, currentTask.Description),
                cancellationToken: cancellationToken);
            await SendAvailableActions(botClient, chatId, cancellationToken);
        }

        else
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: resourceManager.GetTextResource(TextRes.UnknownCommand),
                cancellationToken: cancellationToken);
            return;
        }
        
        userStateManager.SetUserState(chatId, UserState.None);
    }

    private async Task ProcessEnteringTasksStateAsync(ITelegramBotClient botClient, long chatId, string text,
        CancellationToken cancellationToken)
    {
        var newTasks = text.Split(["\r\n", "\r", "\n"], StringSplitOptions.None)
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrEmpty(t))
            .ToList();
        var newTasksCount = newTasks.Count;
        
        var incompleteTasksCount = await taskManager.GetIncompleteTasksCountAsync(chatId);
        if (incompleteTasksCount + newTasksCount > taskManager.MaxIncompleteTasksCount)
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: resourceManager.GetTextResource(TextRes.TooManyTasks),
                cancellationToken: cancellationToken);
            return;
        }

        if (newTasksCount != 0)
        {
            foreach (var taskDescription in newTasks)
            {
                var description = taskDescription;
                if (taskDescription.Length > TaskItem.MaxDescriptionLength)
                    description = taskDescription[..TaskItem.MaxDescriptionLength];
                await taskManager.AddTaskAsync(chatId, description);
            }

            await botClient.SendMessage(
                chatId: chatId,
                text: resourceManager.GetTextResource(TextRes.TasksAdded, newTasks.Count),
                cancellationToken: cancellationToken);
        }
                
        userStateManager.SetUserState(chatId, UserState.None);
    }

    private async Task ProcessSelectingTaskAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        var callbackQuery = update.CallbackQuery;
        if (callbackQuery is null)
            throw new ArgumentException("Callback query is null");
        
        if (!int.TryParse(callbackQuery.Data, out var taskId))
            return;
        var chatId = callbackQuery.Message!.Chat.Id;
        var task = await taskManager.GetTaskByIdAsync(chatId, taskId);

        if (task is not null && !task.IsCompleted)
        {
            currentTaskManager.SetCurrentTask(chatId, task);
            userStateManager.SetUserState(chatId, UserState.DoingTask);
            await botClient.SendMessage(
                chatId: chatId,
                text: resourceManager.GetTextResource(TextRes.TaskSelected, task.Description),
                replyMarkup: GetTaskActionKeyboard(),
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        }
        else
        {
            await botClient.SendMessage(
                chatId: chatId,
                resourceManager.GetTextResource(TextRes.TaskNotFound),
                cancellationToken: cancellationToken);
        }
    }

    private async Task ProcessErrorAsync(ITelegramBotClient botClient, long chatId,
        CancellationToken cancellationToken)
    {
        await botClient.SendMessage(
            chatId: chatId,
            text: resourceManager.GetTextResource(TextRes.Fail),
            cancellationToken: cancellationToken);
    }
    
    public Task HandleErrorAsync(ITelegramBotClient _, Exception exc, CancellationToken cancellationToken)
    {
        // No action is taken because it's processed in the decorator
        return Task.CompletedTask;
    }

    private ReplyKeyboardMarkup GetTaskActionKeyboard()
    {
        return new ReplyKeyboardMarkup([
            [resourceManager.GetTextResource(TextRes.ActionCompleteTask)],
            [resourceManager.GetTextResource(TextRes.ActionReturn)],
            [resourceManager.GetTextResource(TextRes.ActionDeleteTask)]
        ])
        {
            ResizeKeyboard = true
        };
    }

    private async Task SendAvailableActions(ITelegramBotClient botClient, long chatId,
        CancellationToken cancellationToken)
    {
        await botClient.SendMessage(
            chatId: chatId,
            text: resourceManager.GetTextResource(TextRes.AvailableActions),
            ParseMode.Html,
            replyMarkup: new ReplyKeyboardMarkup([
                [resourceManager.GetTextResource(TextRes.ActionAddTasks)],
                [resourceManager.GetTextResource(TextRes.ActionViewTasks)],
                [resourceManager.GetTextResource(TextRes.ActionFinishDay)]
            ])
            {
                ResizeKeyboard = true
            },
            cancellationToken: cancellationToken);
    }
}
