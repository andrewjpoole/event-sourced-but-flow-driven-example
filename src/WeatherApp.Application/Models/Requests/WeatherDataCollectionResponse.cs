using WeatherApp.Domain.Entities;

namespace WeatherApp.Application.Models.Requests;

public record WeatherDataCollectionResponse(Guid RequestId)
{
    public static WeatherDataCollectionResponse FromWeatherDataCollection(WeatherDataCollectionAggregate weatherDataCollectionAggregate)
    {
        return new WeatherDataCollectionResponse(weatherDataCollectionAggregate.StreamId);
    }
}