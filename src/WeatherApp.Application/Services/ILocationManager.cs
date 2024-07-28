using WeatherApp.Domain.Entities;
using WeatherApp.Domain.Outcomes;

namespace WeatherApp.Application.Services;

public interface ILocationManager
{
    Task<OneOf<WeatherDataCollectionAggregate, Failure>> Locate(WeatherDataCollectionAggregate weatherDataCollectionAggregate);
}