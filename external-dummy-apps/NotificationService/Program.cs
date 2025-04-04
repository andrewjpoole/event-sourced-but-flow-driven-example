using Microsoft.Extensions.Options;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using WeatherApp.Application.Models.IntegrationEvents.NotificationEvents;
using WeatherApp.Application.Services;
using WeatherApp.Infrastructure.MessageBus;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace NotificationService;
public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Configuration.AddEnvironmentVariables(prefix: "WeatherApp_");
        builder.Configuration.AddEnvironmentVariables(prefix: "WeatherApp_Notifications_");

        builder.Logging
            .ClearProviders()
            .AddConsole();

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(builder.Environment.ApplicationName))
            .WithLogging(logging => logging
                .AddOtlpExporter());

        var queueHandlerOptions = builder.Configuration.GetSection(ServiceBusInboundQueueHandlerOptions.Name).Get<ServiceBusInboundQueueHandlerOptions>() ??
                                  throw new Exception($"A {nameof(ServiceBusInboundQueueHandlerOptions)} config section is required.");

        IOptions<ServiceBusOptions> inboundServiceBusOptions = Options.Create(queueHandlerOptions);

        builder.Services
            .AddSingleton(inboundServiceBusOptions)
            .ConfigureServiceBusClient(builder.Configuration)
            .AddSingleton(x => TimeProvider.System)
            .AddHostedServiceBusEventListener<UserNotificationEvent, UserNotificationEventHandler>();
        
        var host = builder.Build();
        
        await host.RunAsync();
    }
}

public class UserNotificationEventHandler(ILogger<UserNotificationEventHandler> logger) : IEventHandler<UserNotificationEvent>
{
    public async Task HandleEvent(UserNotificationEvent @event)
    {
        logger.LogInformation("User Notification Sent! Body: {Body}", @event.Body);
        return;
    }
}



