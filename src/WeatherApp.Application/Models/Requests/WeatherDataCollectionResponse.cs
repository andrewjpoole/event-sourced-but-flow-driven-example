using WeatherApp.Domain.Entities;

namespace WeatherApp.Application.Models.Requests;

public record WeatherDataCollectionResponse(Guid RequestId)
{
    public static WeatherDataCollectionResponse FromWeatherDataCollection(WeatherDataCollection weatherDataCollection)
    {
        return new WeatherDataCollectionResponse(weatherDataCollection.RequestId);
    }
}