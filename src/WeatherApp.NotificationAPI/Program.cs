using System.Net;
using Microsoft.AspNetCore.Mvc;
using OneOf.Types;
using WeatherApp.Application.Models.Notifications;

namespace WeatherApp.NotificationAPI;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.WebHost.ConfigureKestrel((context, serverOptions) => serverOptions.Listen(IPAddress.Loopback, 5001));

        builder.Services.AddSingleton<INotificationHandler, NotificationHandler>();
        
        var app = builder.Build();
        
        app.UseHttpsRedirection();
        
        app.MapGet("/v1/notifications", ([FromServices] INotificationHandler handler) 
            => CreateResponseFor(handler.GetNotifications));

        app.MapPost("/v1/notifications/{notificationId}", (
            [FromRoute] Guid notificationId,
            [FromBody] Notification notification,
            [FromServices] INotificationHandler handler)
            => CreateResponseFor(() => handler.HandleNotification(notificationId, notification)));

        static async Task<IResult> CreateResponseFor<TSuccess, TFailure>(Func<Task<OneOf<TSuccess, TFailure>>> handleRequestFunc)
        {
            var result = await handleRequestFunc.Invoke();
            return result.Match(
                success => Results.Ok(success),
                failure => Results.StatusCode((int)HttpStatusCode.InternalServerError)
                );
        }
        
        await app.RunAsync();
    }
}

public interface INotificationHandler
{
    Task<OneOf<List<Notification>, Error>> GetNotifications();
    Task<OneOf<Success, Error>> HandleNotification(Guid notificationId, Notification notification);
}

public class NotificationHandler : INotificationHandler
{
    public Dictionary<Guid, Notification> Notifications { get; } = [];

    public Task<OneOf<List<Notification>, Error>> GetNotifications()
    {
        return Task.FromResult<OneOf<List<Notification>, Error>>(Notifications.Values.ToList());
    }

    public Task<OneOf<Success, Error>> HandleNotification(Guid notificationId, Notification notification)
    {
        Console.WriteLine($"Notification{notificationId}: {notification.Body}");
        Notifications.Add(notificationId, notification);
        return Task.FromResult<OneOf<Success, Error>>(new Success());
    }
}
