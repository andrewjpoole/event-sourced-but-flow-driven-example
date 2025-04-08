using WeatherApp.Application.Models.IntegrationEvents.WeatherModelingEvents;
using WeatherApp.Application.Services;
using WeatherApp.Domain.Outcomes;
using WeatherApp.Application.Models.Requests;
using WeatherApp.Domain.DomainEvents;
using WeatherApp.Domain.Entities;
using WeatherApp.Domain.EventSourcing;
using WeatherApp.Domain.ServiceDefinitions;
using Microsoft.Extensions.Logging;

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
        CollectedWeatherDataModel weatherDataModel, 
        IWeatherDataValidator weatherDataValidator, 
        ILocationManager locationManager)
    {
        logger.LogInformation("Received weather data for location: {Location}", weatherDataLocation);

        if (weatherDataValidator.Validate(weatherDataModel, out var errors) == false)
            return Task.FromResult(OneOf<WeatherDataCollectionResponse, Failure>
                .FromT1(new InvalidRequestFailure(errors)));

        var requestId = Guid.NewGuid();
        logger.LogInformation("Weather data validation passed for location: {Location} RequestId: {RequestId}", requestId, weatherDataLocation);

        return WeatherDataCollectionAggregate.PersistOrHydrate(eventPersistenceService, requestId, 
                Event.Create(new WeatherDataCollectionInitiated(weatherDataModel.ToEntity(), weatherDataLocation), requestId, 1))
            .Then(locationManager.Locate)
            .Then(contributorPaymentService.CreatePendingPayment)
            .Then(weatherModelingService.Submit,   // call to service, async response via integration event 
                (c, f) => contributorPaymentService.RevokePendingPayment(c)) 
            .ToResult(WeatherDataCollectionResponse.FromWeatherDataCollection);
    }
    
    public async Task HandleEvent(ModelingDataRejectedIntegrationEvent dataRejectedIntegrationEvent)
    {
        logger.LogInformation("Received ModelingDataRejectedIntegrationEvent for streamId: {StreamId}", dataRejectedIntegrationEvent.StreamId);

        await WeatherDataCollectionAggregate.Hydrate(eventPersistenceService, dataRejectedIntegrationEvent.StreamId)
            .Then(x => x.AppendModelingDataRejectedEvent(dataRejectedIntegrationEvent.Reason))
            .Then(contributorPaymentService.RevokePendingPayment)
            .ThrowOnFailure(nameof(ModelingDataRejectedIntegrationEvent));
    }

    public async Task HandleEvent(ModelingDataAcceptedIntegrationEvent dataAcceptedIntegrationEvent)
    {
        logger.LogInformation("Received ModelingDataAcceptedIntegrationEvent for streamId: {StreamId}", dataAcceptedIntegrationEvent.StreamId);

        await WeatherDataCollectionAggregate.Hydrate(eventPersistenceService, dataAcceptedIntegrationEvent.StreamId)
            .Then(x => x.AppendModelingDataAcceptedEvent())
            .Then(contributorPaymentService.CommitPendingPayment)
            .Then(x => x.AppendSubmissionCompleteEvent())
            .ThrowOnFailure(nameof(ModelingDataAcceptedIntegrationEvent));
    }

    public async Task HandleEvent(ModelUpdatedIntegrationEvent integrationEvent)
    {
        logger.LogInformation("Received ModelUpdatedIntegrationEvent for streamId: {StreamId}", integrationEvent.StreamId);

        await WeatherDataCollectionAggregate.Hydrate(eventPersistenceService, integrationEvent.StreamId)
            //.Then(x => x.AppendModelUpdatedEvent())
            .Then(eventPersistenceService.AppendModelUpdatedEventAndCreateOutboxItem) // Appends domain event and persists outbox item in single transaction.
            //.Then(notificationService.NotifyModelUpdated)
            .ThrowOnFailure(nameof(ModelUpdatedIntegrationEvent));
    }
}