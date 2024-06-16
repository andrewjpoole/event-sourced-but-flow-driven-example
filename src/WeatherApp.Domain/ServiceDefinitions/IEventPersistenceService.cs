using WeatherApp.Domain.EventSourcing;

namespace WeatherApp.Domain.ServiceDefinitions;
public interface IEventPersistenceService
{
    Task<PersistedEvent> PersistEvent(Event @event);
    Task<List<PersistedEvent>> PersistEvents(IEnumerable<Event> events);

    Task<IEnumerable<PersistedEvent>> FetchEvents(Guid requestId);

    //Task<PersistedEvent> AtomicallyPersistCrossBorderPaymentCompletedEventAndCreateOutboxRecord(Guid requestId, Guid locationId);
    //Task<PersistedEvent> AtomicallyPersistCrossBorderPaymentFailedEventAndCreateOutboxRecord(Guid requestId, Guid locationId, string failureReason);
}
