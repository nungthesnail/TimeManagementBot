using Telegram.Bot;
using Telegram.Bot.Polling;
using TimeManagementBot;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.development.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

const string tokenKey = "TimeManagementBotToken";
var token = config[tokenKey];
if (token is null)
{
    Console.WriteLine("Bot token is null. Can't initialize the bot client.");
    return;
}

var botClient = new TelegramBotClient(token);
var botController = new BotController(new TaskManager());

using var cts = new CancellationTokenSource();

var receiverOptions = new ReceiverOptions();

botClient.StartReceiving(
    botController.HandleUpdateAsync,
    BotController.HandleErrorAsync,
    receiverOptions,
    cts.Token
);

var me = await botClient.GetMe(cancellationToken: cts.Token);

Console.WriteLine($"Start listening @{me.Username}");
Console.Read();

await cts.CancelAsync();
