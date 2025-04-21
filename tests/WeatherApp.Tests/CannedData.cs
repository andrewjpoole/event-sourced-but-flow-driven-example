using WeatherApp.Application.Models.Requests;
using WeatherApp.Domain.DomainEvents;
using WeatherApp.Domain.EventSourcing;

namespace WeatherApp.Tests;

public static class CannedData
{
    private static readonly Random Random = new();
    public static decimal GetRandomWindSpeed() => (decimal)(Random.Next(1, 69) + Random.NextSingle());
    public static string GetRandomWindDirection() => new List<string> { "N", "NE", "E", "SE", "S", "SW", "W", "NW" }[Random.Next(0, 7)];
    public static decimal GetRandomTemperature() => (decimal)(Random.Next(-15, 45) + Random.NextSingle());
    public static decimal GetRandomHumidity() => (decimal)Random.NextSingle();
    
    public static CollectedWeatherDataPointModel GetRandomCollectedWeatherDataModel() =>
        new (
            DateTimeOffset.UtcNow, 
            GetRandomWindSpeed(),
            GetRandomWindDirection(),
            GetRandomTemperature(),
            GetRandomHumidity());

    public static CollectedWeatherDataModel GetRandCollectedWeatherDataModel(int count) => 
        new (Enumerable.Range(0, count).Select(_ => GetRandomCollectedWeatherDataModel()).ToList());

    // Domain events
    public static WeatherDataCollectionInitiated WeatherDataCollectionInitiated(string location, string reference, string idempotencyKey) =>
        new (GetRandCollectedWeatherDataModel(3).ToEntity(), location, reference, idempotencyKey);

    // Scenario domain event collections
    public static List<Event> UpTo_WeatherDataCollectionInitiated(string location, string reference, string idempotencyKey)
    {        
        var version = 1;
        var streamId = Guid.NewGuid();
        return new List<Event> {
            Event.Create(WeatherDataCollectionInitiated(location, reference, idempotencyKey), streamId, version++, null)
        };
    }
}
