using WeatherApp.Domain.ServiceDefinitions;

namespace WeatherApp.Domain.EventSourcing;

public class AggregateRootBase(Guid streamId, List<PersistedEvent> persistedEvents, IEventPersistenceService eventPersistenceService)
{
    public Guid StreamId { get; } = streamId;
    protected List<PersistedEvent> PersistedEvents { get; } = persistedEvents;
    protected IEventPersistenceService EventPersistenceService { get; } = eventPersistenceService;

    public async Task AppendEvent<T>(T eventAsT, int version = -1) where T : IDomainEvent
    {
        if (version == -1)
            version = GetNextExpectedVersion();

        var @event = EventSourcing.Event.Create(eventAsT, StreamId, version);
        var persistedEvent = await EventPersistenceService.PersistEvent(@event);
        PersistedEvents.Add(persistedEvent);
    }

    public async Task AppendEvents(IEnumerable<Event> events)
    {
        if (events.Any(e => e.StreamId != StreamId))
            throw new Exception("All events must have the correct CrossBorderPaymentId");

        var persistedEvents = await EventPersistenceService.PersistEvents(events);
        persistedEvents.AddRange(persistedEvents);
    }

    public void AddPersistedEvent(PersistedEvent persistedEvent)
    {
        if (PersistedEvents.Any(pe => pe.Version == persistedEvent.Version))
            throw new Exception($"Version {persistedEvent.Version} already exists in the aggregate.");

        PersistedEvents.Add(persistedEvent);
    }

    public int GetNextExpectedVersion()
    {
        return PersistedEvents.OrderBy(x => x.Version).Last().Version + 1;
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