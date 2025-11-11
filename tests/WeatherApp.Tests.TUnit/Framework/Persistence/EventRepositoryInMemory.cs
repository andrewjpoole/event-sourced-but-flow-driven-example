using WeatherApp.Domain.DomainEvents;
using WeatherApp.Domain.EventSourcing;
using WeatherApp.Infrastructure.Persistence;
using WeatherApp.Infrastructure.RetryableDapperConnection;

namespace WeatherApp.Tests.TUnit.Framework.Persistence;

public class EventRepositoryInMemory : IEventRepository
{
    public List<PersistedEvent> PersistedEvents { get; } = [];

    public List<FakeDbTransactionWrapped> Transactions { get; } = new();
    
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

    public Task<IEnumerable<PersistedEvent>> FindExistingEventsByIdempotencyKey(string idempotencyKey)
    {
        List<PersistedEvent> matchingInitiatedEvents = [];

        var allExistingInitiatedEvents = PersistedEvents.Where(
            pe => pe.EventClassName == typeof(WeatherDataCollectionInitiated).FullName).ToList();

        foreach (var initiatedEvent in allExistingInitiatedEvents)
        {
            var @event = initiatedEvent.To<WeatherDataCollectionInitiated>();
            if (@event.IdempotencyKey == idempotencyKey)
            {
                matchingInitiatedEvents.Add(initiatedEvent);
            }
        }
        return Task.FromResult((IEnumerable<PersistedEvent>)matchingInitiatedEvents);
    }

    public Task<PersistedEventResult> InsertEvent(Event @event, IDbTransactionWrapped transaction)
    {
        return InsertEvent(@event);
    }

    public IDbTransactionWrapped BeginTransaction()
    {
        var transaction = new FakeDbTransactionWrapped();
        Transactions.Add(transaction);
        return transaction;
    }

    public void InsertExistingEvents(List<Event> domainEvents, TimeProvider timeProvider)
    {
        foreach (var @event in domainEvents)
        {
            var newPersistedEvent = new PersistedEvent(PersistedEvents.Count, @event.StreamId, @event.Version, @event.EventClassName, @event.SerialisedEvent, timeProvider.GetUtcNow());
            PersistedEvents.Add(newPersistedEvent);
        }
    }
}
