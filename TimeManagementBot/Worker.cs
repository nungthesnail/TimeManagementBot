using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Polling;
using TimeManagementBot.Data;
using TimeManagementBot.Interfaces;

namespace TimeManagementBot;

public class Worker(IServiceProvider services) : IHostedService
{
    private ILogger<Worker>? _logger;
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger = services.GetRequiredService<ILoggerFactory>().CreateLogger<Worker>();
        await PrepareAppAsync();
        
        var botController = services.GetRequiredService<IBotController>();
        var telegramClient = services.GetRequiredService<ITelegramBotClient>();
        var receiverOptions = new ReceiverOptions();

        telegramClient.StartReceiving(
            botController.HandleUpdateAsync,
            botController.HandleErrorAsync,
            receiverOptions,
            cancellationToken
        );

        var me = await telegramClient.GetMe(cancellationToken: cancellationToken);

        _logger.LogInformation("Start listening {username}", me.Username);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Stopping worker");
        return Task.CompletedTask;
    }
    
    private async Task PrepareAppAsync()
    {
        _logger?.LogDebug("Preparing the app...");
        await using var scope = services.GetRequiredService<IServiceScopeFactory>().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync(); // Applying migrations on the database
        _logger?.LogDebug("Migrations applied to the database");
    }
}
