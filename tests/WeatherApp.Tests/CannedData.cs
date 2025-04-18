using WeatherApp.Application.Models.Requests;

namespace WeatherApp.Tests;

public static class CannedData
{
    private static readonly Random Random = new();
    public static decimal GetRandomWindSpeed() => (decimal)(Random.Next(1, 69) + Random.NextSingle());
    public static string GetRandomWindDirection() => new List<string> { "N", "NE", "E", "SE", "S", "SW", "W", "NW" }[Random.Next(0, 7)];
    public static decimal GetRandomTemperature() => (decimal)(Random.Next(-15, 45) + Random.NextSingle());
    public static decimal GetRandomHumidity() => (decimal)Random.NextSingle();
    
    public static CollectedWeatherDataPointModel GetRandCollectedWeatherDataModel() =>
        new (
            DateTimeOffset.UtcNow, 
            GetRandomWindSpeed(),
            GetRandomWindDirection(),
            GetRandomTemperature(),
            GetRandomHumidity());
}