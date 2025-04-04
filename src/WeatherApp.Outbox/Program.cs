using WeatherApp.Infrastructure.Persistence;
using WeatherApp.Infrastructure.Outbox;
using WeatherApp.Infrastructure.MessageBus;
using WeatherApp.Domain.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

namespace WeatherApp.Outbox;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddEnvironmentVariables(prefix: "WeatherApp_");

        builder.Logging
            .ClearProviders()
            .AddConsole();

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(builder.Environment.ApplicationName))
            .WithLogging(logging => logging
                .AddOtlpExporter());

        builder.Services
            .AddDatabase(builder.Configuration)
            .AddSingleton(x => TimeProvider.System)
            .ConfigureServiceBusClient(builder.Configuration)
            .AddUniversalMessageSender(builder.Configuration)
            .AddOutboxDispatcherService();

        var app = builder.Build();

        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogStartupMessage("app built");

        app.MapGet("/", () => "Outbox Service is running!");

        await app.RunAsync();
    }
}
