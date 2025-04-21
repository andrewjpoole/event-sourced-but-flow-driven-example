using Microsoft.Extensions.Logging;
using WeatherApp.Domain.EventSourcing;
using WeatherApp.Domain.ServiceDefinitions;
using WeatherApp.Infrastructure.Outbox;
using WeatherApp.Domain.Logging;
using WeatherApp.Domain.Entities;
using WeatherApp.Domain.Outcomes;
using WeatherApp.Application.Models.IntegrationEvents.NotificationEvents;
using WeatherApp.Domain.DomainEvents;

namespace WeatherApp.Infrastructure.Persistence;

public class EventPersistenceService(
    ILogger<EventPersistenceService> logger,
    IEventRepository eventRepository,
    IOutboxItemFactory outboxItemFactory,
    IOutboxRepository outboxRepository, 
    TimeProvider timeProvider) 
    : IEventPersistenceService
{
    protected readonly ILogger<EventPersistenceService> logger = logger;

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

    public async Task<IEnumerable<PersistedEvent>> FetchEvents(Guid streamId)
    {
        var persistedEvents = await eventRepository.FetchEvents(streamId);
        return persistedEvents;
    }

    public async Task<IEnumerable<PersistedEvent>> FindExistingEventsByIdempotencyKey(string idempotencyKey)
    {
        var persistedEvents = await eventRepository.FindExistingEventsByIdempotencyKey(idempotencyKey);
        return persistedEvents;
    }

    private async Task<PersistedEvent> AtomicallyPersistDomainEventAndCreateOutboxRecord<TDomainEvent>(Event @event, OutboxItem outboxItem) where TDomainEvent : class, IDomainEvent
    {
        var transaction = eventRepository.BeginTransaction();
        try
        {
            var result = await eventRepository.InsertEvent(@event, transaction);            
            await outboxRepository.Add(outboxItem);
            transaction.Commit();    

            if(result.TryGetPersistedEvent(out var persistedEvent))
            {
                logger.LogPersistedDomainEventAndOutboxItem(@event.EventClassName, @event.StreamId, outboxItem.Id);
                return persistedEvent;
            }

            throw new Exception($"Unable to persist event. {result.Error}");
        }
        catch (Exception ex)
        {
            logger.LogFailedToPersistDomainEventAndOutboxItem(ex, @event.StreamId);
            transaction.Rollback();
            throw;
        }
    }

    public async Task<OneOf<WeatherDataCollectionAggregate, Failure>> AppendModelUpdatedEventAndCreateOutboxItem(WeatherDataCollectionAggregate weatherDataCollectionAggregate)
    {
        var userNotificationEvent = new UserNotificationEvent(
            "Dear user, your data has been submitted and included in our latest model", 
            weatherDataCollectionAggregate.Reference,
            timeProvider.GetUtcNow());

        var outboxItem = outboxItemFactory.Create(userNotificationEvent, weatherDataCollectionAggregate.StreamId.ToString());
        
        var version = weatherDataCollectionAggregate.GetNextExpectedVersion();
        var @event = Event.Create(new ModelUpdated(), weatherDataCollectionAggregate.StreamId, version);
        
        var persistedEvent = await AtomicallyPersistDomainEventAndCreateOutboxRecord<ModelUpdated>(@event, outboxItem);

        weatherDataCollectionAggregate.AddPersistedEvent(persistedEvent);

        return weatherDataCollectionAggregate;
    }    
}

// public class WeatherAppEventPersistenceService(
//     ILogger<EventPersistenceService> logger,
//     IEventRepository eventRepository) 
//     : EventPersistenceService, IWeatherAppEventPersistenceService
// {
    
// }