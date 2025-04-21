using WeatherApp.Domain.Entities;
using WeatherApp.Domain.EventSourcing;
using WeatherApp.Domain.Outcomes;

namespace WeatherApp.Domain.ServiceDefinitions;
public interface IEventPersistenceService
{
    Task<PersistedEvent> PersistEvent(Event @event);
    Task<List<PersistedEvent>> PersistEvents(IEnumerable<Event> events);
    Task<IEnumerable<PersistedEvent>> FetchEvents(Guid streamId);
    Task<IEnumerable<PersistedEvent>> FindExistingEventsByIdempotencyKey(string idempotencyKey);
    Task PersistFailure(WeatherDataCollectionAggregate weatherDataCollectionAggregate, Failure failure);
    Task<OneOf<WeatherDataCollectionAggregate, Failure>> AppendModelUpdatedEventAndCreateOutboxItem(WeatherDataCollectionAggregate weatherDataCollectionAggregate);    
}

// public interface IWeatherAppEventPersistenceService : IEventPersistenceService {
//     Task<OneOf<WeatherDataCollectionAggregate, Failure>> AppendModelUpdatedEventAndCreateOutboxItem(WeatherDataCollectionAggregate weatherDataCollectionAggregate);
// }
