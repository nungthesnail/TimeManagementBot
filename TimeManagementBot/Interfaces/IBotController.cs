using Telegram.Bot;
using Telegram.Bot.Types;

namespace TimeManagementBot.Interfaces;

public interface IBotController
{
    Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken stoppingToken);

    Task HandleErrorAsync(ITelegramBotClient _, Exception exception,
        CancellationToken stoppingToken);
}
