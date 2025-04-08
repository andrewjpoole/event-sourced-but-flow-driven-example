using WeatherApp.Infrastructure.Persistence;
using WeatherApp.Infrastructure.Outbox;
using WeatherApp.Infrastructure.MessageBus;
using WeatherApp.Domain.Logging;

namespace WeatherApp.Outbox;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults(); 
        builder.AddAzureServiceBusClient(connectionName: "asb");
        builder.AddSqlServerClient(connectionName: "WeatherAppDb");

        builder.Services
            .AddDatabaseConnectionFactory()
            .AddSingleton(x => TimeProvider.System)
            .AddUniversalMessageSender(builder.Configuration)
            .AddOutboxDispatcherService();

        var app = builder.Build();

        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogStartupMessage("app built");

        app.MapGet("/", () => "Outbox Service is running!");

        await app.RunAsync();
    }
}
