// using WeatherApp.Application.Models.Notifications;
// using WeatherApp.Application.Services;
// using WeatherApp.Domain.Entities;
// using WeatherApp.Domain.Outcomes;
// using WeatherApp.Infrastructure.ApiClients.NotificationService;

// namespace WeatherApp.Infrastructure.Notifications;

// public class NotificationService(IRefitClientWrapper<INotificationsClient> notificationsClientWrapper) : INotificationService
// {
//     public async Task<OneOf<WeatherDataCollectionAggregate, Failure>> NotifyModelUpdated(WeatherDataCollectionAggregate weatherDataCollectionAggregate)
//     {
//         var client = notificationsClientWrapper.CreateClient();
//         var response = await client.PostNotification(Guid.NewGuid(), new Notification(DateTime.UtcNow.ToString("s"), $"Model updated for collected weather data with Id: {weatherDataCollectionAggregate.RequestId} at location: {weatherDataCollectionAggregate.Location}"));
//         if (response.IsSuccessStatusCode)
//             return OneOf<WeatherDataCollectionAggregate, Failure>.FromT0(weatherDataCollectionAggregate);

//         throw new Exception("Notification service failed.");
//     }
// }