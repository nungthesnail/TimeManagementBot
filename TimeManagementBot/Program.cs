using Microsoft.EntityFrameworkCore;
using Serilog;
using Telegram.Bot;
using TimeManagementBot;
using TimeManagementBot.Data;
using TimeManagementBot.Implementations;
using TimeManagementBot.Interfaces;

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
const string connStringKey = "Default";
var connectionString = config.GetConnectionString(connStringKey);
if (connectionString is null)
{
    Console.WriteLine("Bot connection string is null. Can't initialize the bot client.");
    return;
}

var hostBuilder = Host.CreateDefaultBuilder();
hostBuilder
    .ConfigureServices(services =>
    {
        services
            .AddHostedService<Worker>()
            .AddDbContext<ApplicationDbContext>(optionsBuilder => optionsBuilder.UseSqlite(connectionString))
            .AddSingleton<IConfiguration>(config)

            .AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(token))

            .AddSingleton<IResourceManager>(static services =>
            {
                const string configAssetsKey = "Assets";
                var config = services.GetRequiredService<IConfiguration>();
                return ResourceManagerFactory.CreateFromConfiguration(config.GetSection(configAssetsKey));
            })
            .AddSingleton<IUserStateManager, UserStateManager>()
            .AddSingleton<ICurrentTaskManager, CurrentTaskManager>()
            .AddScoped<ITaskManager, EfTaskManager>()
            .AddScoped<ISummaryCreator, SummaryCreator>()
            .AddTransient<IBotController, BotControllerDecorator>()
            .BuildServiceProvider();
    })
    .ConfigureLogging((_, logging) =>
    {
        logging.ClearProviders();
        logging.AddSerilog(Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(config)
            .Enrich.FromLogContext()
            .CreateLogger());
    });

var host = hostBuilder.Build();
await host.RunAsync();
