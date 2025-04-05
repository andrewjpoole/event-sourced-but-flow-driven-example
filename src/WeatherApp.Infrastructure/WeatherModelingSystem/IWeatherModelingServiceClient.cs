using WeatherApp.Domain.Entities;
using Refit;

namespace WeatherApp.Infrastructure.ApiClients.WeatherModelingSystem;

public interface IWeatherModelingServiceClient : IDisposable
{
    [Post("/v1/collected-weather-data/{location}/{submissionId}")]
    Task<HttpResponseMessage> PostCollectedData(string location, Guid submissionId, [Body] CollectedWeatherData collectedWeatherData);
}