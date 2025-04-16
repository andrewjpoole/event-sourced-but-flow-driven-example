using WeatherApp.Infrastructure.Persistence;
using WeatherApp.Infrastructure.Outbox;
using WeatherApp.Infrastructure.Messaging;
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
            .AddOutboxDispatcherService(builder.Configuration);

        var app = builder.Build();

        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogStartupMessage("app built");

        app.MapGet("/", () => "Outbox Service is running!");

        var pollingInterval = builder.Configuration.GetValue<int>("OutboxProcessorOptions:IntervalBetweenBatchesInSeconds");
        logger.LogInformation("polling interval: {Interval}", pollingInterval);

        await app.RunAsync();
    }
}
