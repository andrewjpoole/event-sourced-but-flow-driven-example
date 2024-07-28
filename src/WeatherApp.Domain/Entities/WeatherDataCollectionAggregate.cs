using WeatherApp.Domain.BusinessRules;
using WeatherApp.Domain.DomainEvents;
using WeatherApp.Domain.EventSourcing;
using WeatherApp.Domain.Exceptions;
using WeatherApp.Domain.Outcomes;
using WeatherApp.Domain.ServiceDefinitions;

namespace WeatherApp.Domain.Entities;

// ToDo:now re-organise into vertical slices?
public class WeatherDataCollectionAggregate : AggregateRootBase
{
    public WeatherDataCollectionAggregate(Guid requestId, List<PersistedEvent> persistedEvents, IEventPersistenceService eventPersistenceService)
        : base(requestId, persistedEvents, eventPersistenceService)
    {
        Check.NotNull(PersistedEvents);

        var initiatedEvent = PersistedEvents.To<WeatherDataCollectionInitiated>();
        Check.NotNull(initiatedEvent);

        Check.NotNull(initiatedEvent.Data);
        Check.NotNull(initiatedEvent.Location);
    }

    // Properties from events guaranteed to be present because they come from the initiated event...
    public CollectedWeatherData Data => PersistedEvents.To<WeatherDataCollectionInitiated>()!.Data;
    public string Location => PersistedEvents.To<WeatherDataCollectionInitiated>()!.Location;

    // Properties from events which may not yet have happened, null if not yet happened.
    public Guid? LocationId => PersistedEvents.To<LocationIdFound>()!.LocationId;
    public Guid? ModelingSubmissionId => PersistedEvents.To<SubmittedToModeling>()!.SubmissionId;

    // Flags from events, used to skip tasks which are already complete i.e. on retry.
    public bool ModelingDataRejected => EventHasHappened<ModelingDataRejected>();
    bool ModelingDataAccepted => EventHasHappened<ModelingDataAccepted>();
    bool SubmissionCompleted => EventHasHappened<SubmissionComplete>();
    bool ModelUpdated => EventHasHappened<ModelUpdated>();

    public static async Task<OneOf<WeatherDataCollectionAggregate, Failure>> Hydrate(IEventPersistenceService eventPersistenceService, Guid requestId)
    {
        var persistedEvents = (await eventPersistenceService.FetchEvents(requestId)).ToList();

        if (persistedEvents.Count == 0)
            throw new ExpectedEventsNotFoundException();

        return new WeatherDataCollectionAggregate(requestId, persistedEvents, eventPersistenceService);
    }

    public static async Task<OneOf<WeatherDataCollectionAggregate, Failure>> PersistOrHydrate(IEventPersistenceService eventPersistenceService, Guid requestId, Event initialEvent)
    {
        var existingPersistedEvents = (await eventPersistenceService.FetchEvents(requestId)).ToList();

        if (existingPersistedEvents.Count != 0)
            return new WeatherDataCollectionAggregate(requestId, existingPersistedEvents, eventPersistenceService);
        
        var initialEvents = new List<Event>
        {
            initialEvent
        };
        var persistedInitialEvents = await eventPersistenceService.PersistEvents(initialEvents);

        var payment = new WeatherDataCollectionAggregate(requestId, persistedInitialEvents, eventPersistenceService);
        return payment;
    }

    public async Task<OneOf<WeatherDataCollectionAggregate, Failure>> AppendSubmissionCompleteEvent()
    {
        await AppendEvent(new SubmissionComplete());
        return this;
    }

    public async Task<OneOf<WeatherDataCollectionAggregate, Failure>> AppendModelingDataAcceptedEvent()
    {
        await AppendEvent(new ModelingDataAccepted());
        return this;
    }

    public async Task<OneOf<WeatherDataCollectionAggregate, Failure>> AppendModelingDataRejectedEvent(string reason)
    {
        await AppendEvent(new ModelingDataRejected(reason));
        return this;
    }

    public async Task<OneOf<WeatherDataCollectionAggregate, Failure>> AppendModelUpdatedEvent()
    {
        await AppendEvent(new ModelUpdated());
        return this;
    }
}