using WeatherApp.Domain.BusinessRules;
using WeatherApp.Domain.DomainEvents;
using WeatherApp.Domain.EventSourcing;
using WeatherApp.Domain.Exceptions;
using WeatherApp.Domain.Outcomes;
using WeatherApp.Domain.ServiceDefinitions;
using WeatherApp.Domain.ValueObjects;

namespace WeatherApp.Domain.Entities;

public class WeatherDataCollectionAggregate : AggregateRootBase
{
    public WeatherDataCollectionAggregate(Guid streamId, List<PersistedEvent> persistedEvents, IEventPersistenceService eventPersistenceService)
        : base(streamId, persistedEvents, eventPersistenceService)
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
    public string Reference => PersistedEvents.To<WeatherDataCollectionInitiated>()!.Reference;

    // Properties from events which may not yet have happened, null if not yet happened.
    public Guid? LocationId => PersistedEvents.To<LocationIdFound>()!.LocationId;
    public PendingContributorPayment? PendingPayment => PersistedEvents.To<PendingContributorPaymentPosted>()?.PendingContributorPayment;
    public string? ModelingDataRejectedReason => PersistedEvents.To<ModelingDataRejected>()?.Reason;
    public Guid? ModelingSubmissionId => PersistedEvents.To<SubmittedToModeling>()!.SubmissionId;

    // Flags from events, used to skip tasks which are already complete i.e. on retry.
    public bool ModelingDataRejected => EventHasHappened<ModelingDataRejected>();
    public bool ModelingDataAccepted => EventHasHappened<ModelingDataAccepted>();
    public bool SubmissionCompleted => EventHasHappened<SubmissionComplete>();
    public bool ModelUpdated => EventHasHappened<ModelUpdated>();
    public bool PendingPaymentRevoked => EventHasHappened<PendingContributorPaymentRevoked>();
    public bool PendingPaymentCommitted => EventHasHappened<PendingContributorPaymentCommitted>();

    public static async Task<OneOf<WeatherDataCollectionAggregate, Failure>> Hydrate(IEventPersistenceService eventPersistenceService, Guid streamId)
    {
        var persistedEvents = (await eventPersistenceService.FetchEvents(streamId)).ToList();

        if (persistedEvents.Count == 0)
            throw new ExpectedEventsNotFoundException();

        return new WeatherDataCollectionAggregate(streamId, persistedEvents, eventPersistenceService);
    }

    public static async Task<OneOf<WeatherDataCollectionAggregate, Failure>> PersistOrHydrate(
        IEventPersistenceService eventPersistenceService, 
        Func<Guid, List<Event>> provideInitialEvents, 
        string idempotencyKey, 
        Func<List<PersistedEvent>, bool> idempotencyCheck)
    {
        // Idempotency
        // 1. take idempotencyKey and use it to lookup existing events.
        // 2. if none are found create a new aggregate and persist the initial event.
        // 3. if some are found, check if the idempotencyCheck passes. If it does, return the existing aggregate/Failure.
        //    - may need to record domain events for permanent Failures in the API flow, so same result can be returned.
        // 4. if idempotencyCheck fails, return a conflict failure.
        Guid streamId;
        var existingEventsByIdempotencyKey = await eventPersistenceService.FindExistingEventsByIdempotencyKey(idempotencyKey);
        var existingEventsByIdempotencyKeyList = existingEventsByIdempotencyKey.ToList();

        // New Aggregate...
        if(existingEventsByIdempotencyKey.Count() == 0)
        {
            streamId = Guid.NewGuid();
            var initialEvents = provideInitialEvents(streamId);
            var persistedInitialEvents = await eventPersistenceService.PersistEvents(initialEvents);            
            return new WeatherDataCollectionAggregate(streamId, persistedInitialEvents, eventPersistenceService);
        }

        // Existing Aggregate...
        streamId = existingEventsByIdempotencyKeyList.Last().StreamId;
        var existingEvents = await eventPersistenceService.FetchEvents(streamId);     
        
        // Check if this a duplicate request or a new request using an existing idempotency key.
        var idempotencyCheckPassed = idempotencyCheck(existingEventsByIdempotencyKeyList);

        if(idempotencyCheckPassed)
        {
            // Return the existing aggregate for a retry.
            var existingAggregate = new WeatherDataCollectionAggregate(streamId, existingEvents.ToList(), eventPersistenceService);            
            return existingAggregate;
        }
        else
        {
            var failure = new AlreadyProcessedFailure($"RequestId already processed and idempotency checks failed");

            return OneOf<WeatherDataCollectionAggregate, Failure>
            .FromT1(failure);
        }
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

    public async Task<OneOf<WeatherDataCollectionAggregate, Failure>> AppendPendingContributorPaymentEvent(PendingContributorPayment pendingPayment)
    {
        await AppendEvent(new PendingContributorPaymentPosted(pendingPayment));
        return this;
    }

    public async Task<OneOf<WeatherDataCollectionAggregate, Failure>> AppendRevokedContributorPaymentEvent(Guid paymentId)
    {
        await AppendEvent(new PendingContributorPaymentRevoked(paymentId));
        return this;
    }

    public async Task<OneOf<WeatherDataCollectionAggregate, Failure>> AppendCommittedContributorPaymentEvent(Guid paymentId)
    {
        await AppendEvent(new PendingContributorPaymentCommitted(paymentId));
        return this;
    }
    
}