using WeatherApp.Application.Models.Requests;
using WeatherApp.Application.Services;
using WeatherApp.Domain.Outcomes;

namespace WeatherApp.Application.Orchestration;

public interface ISubmitWeatherDataCommandHandler
{
    Task<OneOf<WeatherDataCollectionResponse, Failure>> HandleSubmitWeatherDataCommand(
        string weatherDataLocation, 
        string reference, 
        Guid requestId, 
        CollectedWeatherDataModel weatherDataModel, 
        IWeatherDataValidator weatherDataValidator, 
        ILocationManager locationManager);
}