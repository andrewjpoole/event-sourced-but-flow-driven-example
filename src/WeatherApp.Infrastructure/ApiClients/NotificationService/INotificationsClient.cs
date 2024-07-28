using Refit;
using WeatherApp.Application.Models.Notifications;

namespace WeatherApp.Infrastructure.ApiClients.NotificationService;

public interface INotificationsClient : IDisposable
{
    [Post("/v1/notifications/{notificationId}")]
    Task<HttpResponseMessage> PostNotification(Guid notificationId, [Body] Notification notification);
}