using WeatherApp.Domain.Entities;
using WeatherApp.Domain.Outcomes;

namespace WeatherApp.Application.Services;

public interface INotificationService
{
    Task<OneOf<WeatherDataCollectionAggregate, Failure>> NotifyModelUpdated(WeatherDataCollectionAggregate weatherDataCollectionAggregate);
}