using WeatherApp.Application.Services;
using WeatherApp.Domain.DomainEvents;
using WeatherApp.Domain.Entities;
using WeatherApp.Domain.Outcomes;

namespace WeatherApp.Infrastructure.LocationManager;

public class LocationManager : ILocationManager
{
    private readonly Dictionary<string, Guid> knownLocations = [];

    public async Task<OneOf<WeatherDataCollection, Failure>> Locate(WeatherDataCollection weatherDataCollection)
    {
        if (!knownLocations.ContainsKey(weatherDataCollection.Location))
            knownLocations.Add(weatherDataCollection.Location, Guid.NewGuid());
        
        await weatherDataCollection.AppendEvent(new LocationIdFound(knownLocations[weatherDataCollection.Location]));

        return weatherDataCollection;
    }
}