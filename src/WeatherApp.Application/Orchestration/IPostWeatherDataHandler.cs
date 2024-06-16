using WeatherApp.Application.Models.Requests;
using WeatherApp.Application.Services;
using WeatherApp.Domain.Outcomes;

namespace WeatherApp.Application.Orchestration;

public interface IPostWeatherDataHandler
{
    Task<OneOf<WeatherDataCollectionResponse, Failure>> HandlePostWeatherData(string weatherDataLocation, CollectedWeatherDataModel weatherDataModel, 
        IWeatherDataValidator weatherDataValidator, ILocationManager locationManager);
}