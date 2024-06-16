using WeatherApp.Application.Services;
using WeatherApp.Domain.Entities;
using WeatherApp.Domain.Outcomes;

namespace WeatherApp.Infrastructure.Notifications;

public class NotificationService : INotificationService
{
    public Task<OneOf<WeatherDataCollection, Failure>> NotifyModelUpdated(WeatherDataCollection weatherDataCollection)
    {
        // todo: simulate some notification maybe?
        return Task.FromResult(OneOf<WeatherDataCollection, Failure>.FromT0(weatherDataCollection));
    }
}