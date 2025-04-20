using WeatherApp.Application.Models.IntegrationEvents.WeatherModelingEvents;
using WeatherApp.Application.Services;
using WeatherApp.Domain.Outcomes;
using WeatherApp.Application.Models.Requests;
using WeatherApp.Domain.DomainEvents;
using WeatherApp.Domain.Entities;
using WeatherApp.Domain.EventSourcing;
using WeatherApp.Domain.ServiceDefinitions;
using Microsoft.Extensions.Logging;
using WeatherApp.Domain.Logging;
using System.Diagnostics;

namespace WeatherApp.Application.Orchestration;

public class CollectedWeatherDataOrchestrator(
    ILogger<CollectedWeatherDataOrchestrator> logger,
    IEventPersistenceService eventPersistenceService,
    IWeatherModelingService weatherModelingService,
    IContributorPaymentService contributorPaymentService)
    :
        ISubmitWeatherDataCommandHandler,
        IEventHandler<ModelingDataAcceptedIntegrationEvent>,
        IEventHandler<ModelingDataRejectedIntegrationEvent>,
        IEventHandler<ModelUpdatedIntegrationEvent>
{
    public Task<OneOf<WeatherDataCollectionResponse, Failure>> HandleSubmitWeatherDataCommand(
        string weatherDataLocation, 
        string reference,
        Guid requestId,
        CollectedWeatherDataModel weatherDataModel, 
        IWeatherDataValidator weatherDataValidator, 
        ILocationManager locationManager)
    {
        logger.LogReceivedWeatherData(reference, weatherDataLocation);

        var activity = Activity.Current;
        if (activity != null)
        {
            activity.SetTag("weatherDataLocation", weatherDataLocation);
            activity.SetTag("reference", reference);
        }

        if (weatherDataValidator.Validate(weatherDataModel, out var errors) == false)
            return Task.FromResult(OneOf<WeatherDataCollectionResponse, Failure>
                .FromT1(new InvalidRequestFailure(errors)));
        
        var streamId = Guid.NewGuid();
        logger.LogWeatherDataValidationPassed(weatherDataLocation, requestId);

        return WeatherDataCollectionAggregate.PersistOrHydrate(eventPersistenceService, streamId, 
                Event.Create(new WeatherDataCollectionInitiated(weatherDataModel.ToEntity(), weatherDataLocation, reference, requestId), streamId, 1))
            .Then(locationManager.Locate)
            .Then(contributorPaymentService.CreatePendingPayment)
            .Then(weatherModelingService.Submit,   // call to service, async response via integration event 
                (c, f) => contributorPaymentService.RevokePendingPayment(c)) 
            .ToResult(WeatherDataCollectionResponse.FromWeatherDataCollection);
    }
    
    public async Task HandleEvent(ModelingDataRejectedIntegrationEvent dataRejectedIntegrationEvent)
    {
        logger.LogReceivedModelingDataRejectedEvent(dataRejectedIntegrationEvent.StreamId);

        await WeatherDataCollectionAggregate.Hydrate(eventPersistenceService, dataRejectedIntegrationEvent.StreamId)
            .Then(x => x.AppendModelingDataRejectedEvent(dataRejectedIntegrationEvent.Reason))
            .Then(contributorPaymentService.RevokePendingPayment)
            .ThrowOnFailure(nameof(ModelingDataRejectedIntegrationEvent));
    }

    public async Task HandleEvent(ModelingDataAcceptedIntegrationEvent dataAcceptedIntegrationEvent)
    {
        logger.LogReceivedModelingDataAcceptedEvent(dataAcceptedIntegrationEvent.StreamId);

        await WeatherDataCollectionAggregate.Hydrate(eventPersistenceService, dataAcceptedIntegrationEvent.StreamId)
            .Then(x => x.AppendModelingDataAcceptedEvent())
            .Then(contributorPaymentService.CommitPendingPayment)
            .Then(x => x.AppendSubmissionCompleteEvent())
            .ThrowOnFailure(nameof(ModelingDataAcceptedIntegrationEvent));
    }

    public async Task HandleEvent(ModelUpdatedIntegrationEvent integrationEvent)
    {
        logger.LogReceivedModelUpdatedEvent(integrationEvent.StreamId);

        await WeatherDataCollectionAggregate.Hydrate(eventPersistenceService, integrationEvent.StreamId)
            .Then(eventPersistenceService.AppendModelUpdatedEventAndCreateOutboxItem) // Appends domain event and persists outbox item in single transaction.
            .ThrowOnFailure(nameof(ModelUpdatedIntegrationEvent));
    }
}