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

        builder.AddServiceDefaults();
        builder.AddAzureServiceBusClient(connectionName: "asb");

        builder.Services
            .AddServiceBusInboundQueueHandlerOptions(builder.Configuration)
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



