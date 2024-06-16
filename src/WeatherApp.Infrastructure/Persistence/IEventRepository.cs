using WeatherApp.Domain.EventSourcing;

namespace WeatherApp.Infrastructure.Persistence;

public interface IEventRepository
{
    Task<PersistedEvent> InsertEvent(Guid groupingId, string eventClassName, string serialisedEvent);

    Task<List<PersistedEvent>> InsertEvents(IList<(Guid groupingId, string eventClassName, string serialisedEvent)> events);

    Task<IEnumerable<PersistedEvent>> FetchEvents(Guid requestId);
}