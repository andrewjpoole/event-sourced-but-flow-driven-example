using Microsoft.Extensions.Logging;
using WeatherApp.Domain.EventSourcing;
using WeatherApp.Domain.ServiceDefinitions;

namespace WeatherApp.Infrastructure.Persistence;

public class EventPersistenceService(
    ILogger<EventPersistenceService> logger,
    IEventRepository eventRepository) 
    : IEventPersistenceService
{
    private readonly ILogger<EventPersistenceService> logger = logger;

    public async Task<PersistedEvent> PersistEvent(Event @event)
    {
        var result = await eventRepository.InsertEvent(@event);
        if(result.TryGetPersistedEvent(out var persistedEvent))
            return persistedEvent;
        
        throw new Exception($"Unable to persist event. {result.Error}");
    }

    public async Task<List<PersistedEvent>> PersistEvents(IEnumerable<Event> events)
    {
        var result = await eventRepository.InsertEvents(events.ToList());
        if(result.TryGetPersistedEvents(out var persistedEvents))
            return persistedEvents;

        throw new Exception($"Unable to persist events. {result.Error}");
    }

    public async Task<IEnumerable<PersistedEvent>> FetchEvents(Guid requestId)
    {
        var persistedEvents = await eventRepository.FetchEvents(requestId);
        return persistedEvents;
    }
}