# EventRepositoryInMemory template

Purpose: implement the real event repository interface in memory so component tests can seed and inspect persisted events directly. Adapt names like `PersistedEvent`, result wrappers, and the idempotency lookup event type to the target app.

```csharp
using {Namespace}.Domain.DomainEvents;
using {Namespace}.Domain.EventSourcing;
using {Namespace}.Infrastructure.Persistence;
using {Namespace}.Infrastructure.RetryableDapperConnection;

namespace {Namespace}.Tests.TUnit.Framework.Persistence;

public sealed class EventRepositoryInMemory : IEventRepository
{
    public List<PersistedEvent> PersistedEvents { get; } = [];
    public List<FakeDbTransactionWrapped> Transactions { get; } = [];

    public Task<PersistedEventResult> InsertEvent(Event @event)
    {
        var persistedEvent = new PersistedEvent(
            PersistedEvents.Count,
            @event.StreamId,
            @event.Version,
            @event.EventClassName,
            @event.SerialisedEvent,
            DateTime.UtcNow);

        PersistedEvents.Add(persistedEvent);
        return Task.FromResult(PersistedEventResult.FromSuccess(persistedEvent));
    }

    public Task<PersistedEventResult> InsertEvent(Event @event, IDbTransactionWrapped transaction)
        => InsertEvent(@event);

    public Task<PersistedEventsResult> InsertEvents(IList<Event> events)
    {
        var persistedEvents = new List<PersistedEvent>();

        foreach (var @event in events)
        {
            var persistedEvent = new PersistedEvent(
                PersistedEvents.Count,
                @event.StreamId,
                @event.Version,
                @event.EventClassName,
                @event.SerialisedEvent,
                DateTime.UtcNow);

            PersistedEvents.Add(persistedEvent);
            persistedEvents.Add(persistedEvent);
        }

        return Task.FromResult(PersistedEventsResult.FromSuccess(persistedEvents));
    }

    public Task<IEnumerable<PersistedEvent>> FetchEvents(Guid streamId)
        => Task.FromResult(PersistedEvents.Where(x => x.StreamId == streamId).AsEnumerable());

    public Task<IEnumerable<PersistedEvent>> FindExistingEventsByIdempotencyKey(string idempotencyKey)
    {
        var matches = new List<PersistedEvent>();

        // IMPORTANT: adapt {InitiatedEventType} to the domain event that actually carries the idempotency key.
        var initiatedEvents = PersistedEvents
            .Where(x => x.EventClassName == typeof({InitiatedEventType}).FullName)
            .ToList();

        foreach (var persistedEvent in initiatedEvents)
        {
            var domainEvent = persistedEvent.To<{InitiatedEventType}>();
            if (domainEvent.IdempotencyKey == idempotencyKey)
            {
                matches.Add(persistedEvent);
            }
        }

        return Task.FromResult(matches.AsEnumerable());
    }

    public void InsertExistingEvents(List<Event> events, TimeProvider timeProvider)
    {
        foreach (var @event in events)
        {
            PersistedEvents.Add(new PersistedEvent(
                PersistedEvents.Count,
                @event.StreamId,
                @event.Version,
                @event.EventClassName,
                @event.SerialisedEvent,
                timeProvider.GetUtcNow()));
        }
    }

    public IDbTransactionWrapped BeginTransaction()
    {
        var transaction = new FakeDbTransactionWrapped();
        Transactions.Add(transaction);
        return transaction;
    }
}
```

Note:

`FindExistingEventsByIdempotencyKey` is intentionally domain-specific. Change it to inspect whichever event in the target system actually carries the idempotency key or duplicate-detection value.
