using WeatherApp.Domain.Entities;
using WeatherApp.Domain.Outcomes;

namespace WeatherApp.Application.Services;

public interface INotificationService
{
    Task<OneOf<WeatherDataCollection, Failure>> NotifyModelUpdated(WeatherDataCollection weatherDataCollection);
}