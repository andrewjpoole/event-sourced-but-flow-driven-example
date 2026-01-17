using System.Diagnostics;
using WeatherApp.Application.Models.IntegrationEvents.NotificationEvents;
using WeatherApp.Application.Services;
using WeatherApp.Infrastructure.Messaging;

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
            .AddSingleton<SentNotifications>()
            .AddServiceBusInboundQueueHandlerOptions(builder.Configuration)
            .AddSingleton(x => TimeProvider.System)
            .AddHostedServiceBusEventListener<UserNotificationEvent, UserNotificationEventHandler>();
        
        var host = builder.Build();
        
        await host.RunAsync();
    }
}

public class UserNotificationEventHandler(ILogger<UserNotificationEventHandler> logger, SentNotifications sentNotifications) : IEventHandler<UserNotificationEvent>
{
    private static readonly ActivitySource Activity = new(nameof(UserNotificationEventHandler));
    public async Task HandleEvent(UserNotificationEvent @event)
    {
        

        sentNotifications.Add(@event);

        logger.LogInformation("User Notification Sent! {Reference}\nBody: {Body}\n@{Timestamp}", @event.Body, @event.Reference, @event.Timestamp);

        return;
    }
}

public class SentNotifications : List<UserNotificationEvent>
{
    public SentNotifications() : base() {}    
}