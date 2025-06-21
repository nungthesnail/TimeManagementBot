using Telegram.Bot;
using Telegram.Bot.Types;
using TimeManagementBot.Interfaces;

namespace TimeManagementBot.Implementations;

public class BotControllerDecorator(IServiceProvider services) : IBotController
{
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken stoppingToken)
    {
        var requestScope = CreateControllerImplementation();
        try
        {
            await requestScope.Controller.HandleUpdateAsync(botClient, update, stoppingToken);
        }
        catch (Exception exc)
        {
            var chatId = update.Message?.Chat.Id ?? update.CallbackQuery?.Message?.Chat.Id;
            var errorMessageSent = chatId is not null && await TrySendErrorMessage(botClient, chatId.Value);
            
            var logger = services
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger<BotControllerDecorator>();
            
            logger.LogError(exc, "An error occured while handling update. Error message is {sent} sent",
                errorMessageSent ? "not" : "");
        }
        finally
        {
            await requestScope.Resources.DisposeAsync();
        }
    }

    private static async Task<bool> TrySendErrorMessage(ITelegramBotClient botClient, long chatId)
    {
        try
        {
            await botClient.SendMessage(chatId, "Кажется, что-то пошло не так. Попробуй еще раз");
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private (IBotController Controller, IAsyncDisposable Resources) CreateControllerImplementation()
    {
        var scope = services
            .GetRequiredService<IServiceScopeFactory>()
            .CreateAsyncScope();
        var taskManager = scope.ServiceProvider.GetRequiredService<ITaskManager>();
        var userStateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();
        var currentTaskManager = scope.ServiceProvider.GetRequiredService<ICurrentTaskManager>();
        var summaryCreator = scope.ServiceProvider.GetRequiredService<ISummaryCreator>();
        var resourceManager = scope.ServiceProvider.GetRequiredService<IResourceManager>();
        var controller = new BotController(
            taskManager, userStateManager, summaryCreator, currentTaskManager, resourceManager);
        
        return (controller, scope);
    }

    public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken stoppingToken)
    {
        var logger = services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger<BotControllerDecorator>();
        logger.LogError(exception, "Something failed with telegram client");
        
        var requestScope = CreateControllerImplementation();
        try
        {
            await requestScope.Controller.HandleErrorAsync(botClient, exception, stoppingToken);
        }
        catch (Exception exc)
        {
            logger.LogError(exc, "An error occured while handling telegram error");
        }
        finally
        {
            await requestScope.Resources.DisposeAsync();
        }
    }
}
