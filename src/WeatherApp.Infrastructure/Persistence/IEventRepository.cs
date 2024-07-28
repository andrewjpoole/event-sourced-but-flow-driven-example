using WeatherApp.Domain.EventSourcing;

namespace WeatherApp.Infrastructure.Persistence;

public interface IEventRepository
{
    Task<PersistedEventResult> InsertEvent(Event @event);
    Task<PersistedEventsResult> InsertEvents(IList<Event> events);
    Task<IEnumerable<PersistedEvent>> FetchEvents(Guid streamId);
}