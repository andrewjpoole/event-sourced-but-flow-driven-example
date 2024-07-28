using WeatherApp.Domain.ServiceDefinitions;

namespace WeatherApp.Domain.EventSourcing;

public class AggregateRootBase(Guid requestId, List<PersistedEvent> persistedEvents, IEventPersistenceService eventPersistenceService)
{
    public Guid RequestId { get; } = requestId;
    protected List<PersistedEvent> PersistedEvents { get; } = persistedEvents;
    protected IEventPersistenceService EventPersistenceService { get; } = eventPersistenceService;

    public async Task AppendEvent<T>(T eventAsT, int version = -1) where T : IDomainEvent
    {
        if (version == -1)
            version = GetNextExpectedVersion();

        var @event = EventSourcing.Event.Create(eventAsT, RequestId, version);
        var persistedEvent = await EventPersistenceService.PersistEvent(@event);
        PersistedEvents.Add(persistedEvent);
    }

    public async Task AppendEvents(IEnumerable<Event> events)
    {
        if (events.Any(e => e.StreamId != this.RequestId))
            throw new Exception("All events must have the correct CrossBorderPaymentId");

        var persistedEvents = await EventPersistenceService.PersistEvents(events);
        persistedEvents.AddRange(persistedEvents);
    }

    private int GetNextExpectedVersion()
    {
        return PersistedEvents.OrderBy(x => x.Version).Last().Version;
    }

    public bool EventHasHappened<T>()
    {
        var eventClassName = typeof(T).FullName;
        return PersistedEvents.Any(pe => pe.EventClassName == eventClassName);
    }

    public T? Event<T>()
    {
        return PersistedEvents.To<T>();
    }

    public PersistedEvent? PersistedEvent<T>()
    {
        var eventClassName = typeof(T).FullName;
        return PersistedEvents.LastOrDefault(pe => pe.EventClassName == eventClassName);
    }
}