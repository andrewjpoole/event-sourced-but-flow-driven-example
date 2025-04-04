using WeatherApp.Domain.EventSourcing;
using WeatherApp.Infrastructure.RetryableDapperConnection;

namespace WeatherApp.Infrastructure.Persistence;

public class EventRepositoryInMemory : IEventRepository
{
    public List<PersistedEvent> PersistedEvents { get; } = [];
    
    public Task<PersistedEventResult> InsertEvent(Event @event)
    {
        var newPersistedEvent = new PersistedEvent(PersistedEvents.Count, @event.StreamId, @event.Version, @event.EventClassName, @event.SerialisedEvent, DateTime.UtcNow);
        PersistedEvents.Add(newPersistedEvent);
        return Task.FromResult(PersistedEventResult.FromSuccess(newPersistedEvent));
    }

    public Task<PersistedEventsResult> InsertEvents(IList<Event> events)
    {
        var newPersistedEvents = new List<PersistedEvent>();
        foreach (var @event in events)
        {
            var newPersistedEvent = new PersistedEvent(PersistedEvents.Count, @event.StreamId, @event.Version, @event.EventClassName, @event.SerialisedEvent, DateTime.UtcNow);
            PersistedEvents.Add(newPersistedEvent);
            newPersistedEvents.Add(newPersistedEvent);
        }
        return Task.FromResult(PersistedEventsResult.FromSuccess(newPersistedEvents));
    }
    public Task<IEnumerable<PersistedEvent>> FetchEvents(Guid streamId)
    {
        return Task.FromResult(PersistedEvents.Where(pe => pe.StreamId == streamId));
    }

    public Task<PersistedEventResult> InsertEvent(Event @event, IDbTransactionWrapped transaction)
    {
        throw new NotImplementedException();
    }

    public IDbTransactionWrapped BeginTransaction()
    {
        throw new NotImplementedException();
    }
}



