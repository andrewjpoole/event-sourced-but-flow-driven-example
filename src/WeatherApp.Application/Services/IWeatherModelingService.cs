using WeatherApp.Domain.Entities;
using WeatherApp.Domain.Outcomes;

namespace WeatherApp.Application.Services;

public interface IWeatherModelingService
{
    Task<OneOf<WeatherDataCollection, Failure>> Submit(WeatherDataCollection weatherDataCollection);
}