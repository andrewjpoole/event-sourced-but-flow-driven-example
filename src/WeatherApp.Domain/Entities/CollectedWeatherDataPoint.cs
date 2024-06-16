using WeatherApp.Domain.ValueObjects;

namespace WeatherApp.Domain.Entities;

public record CollectedWeatherDataPoint(
    Guid Id,
    DateTimeOffset time,
    WindSpeed WindSpeed,
    WindDirection WindDirection,
    Temperature Temperature,
    Humidity Humidity
);