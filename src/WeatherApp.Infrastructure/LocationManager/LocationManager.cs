using WeatherApp.Application.Services;
using WeatherApp.Domain.DomainEvents;
using WeatherApp.Domain.Entities;
using WeatherApp.Domain.Outcomes;

namespace WeatherApp.Infrastructure.LocationManager;

public class LocationManager : ILocationManager
{
    private readonly Dictionary<string, Guid> knownLocations = [];

    public async Task<OneOf<WeatherDataCollectionAggregate, Failure>> Locate(WeatherDataCollectionAggregate weatherDataCollectionAggregate)
    {
        if (!knownLocations.ContainsKey(weatherDataCollectionAggregate.Location))
            knownLocations.Add(weatherDataCollectionAggregate.Location, Guid.NewGuid());
        
        await weatherDataCollectionAggregate.AppendEvent(new LocationIdFound(knownLocations[weatherDataCollectionAggregate.Location]));

        return weatherDataCollectionAggregate;
    }
}