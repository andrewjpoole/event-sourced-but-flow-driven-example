using WeatherApp.Application.Models.IntegrationEvents.WeatherModelingEvents;
using WeatherApp.Application.Services;
using WeatherApp.Domain.Outcomes;
using WeatherApp.Application.Models.Requests;
using WeatherApp.Domain.DomainEvents;
using WeatherApp.Domain.Entities;
using WeatherApp.Domain.EventSourcing;
using WeatherApp.Domain.ServiceDefinitions;
using WeatherApp.Application.Models.IntegrationEvents.NotificationEvents;

namespace WeatherApp.Application.Orchestration;

public class CollectedWeatherDataOrchestrator(
    IEventPersistenceService eventPersistenceService,
    IWeatherModelingService weatherModelingService,
    //INotificationService notificationService,
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
        if (weatherDataValidator.Validate(weatherDataModel, out var errors) == false)
            return Task.FromResult(OneOf<WeatherDataCollectionResponse, Failure>
                .FromT1(new InvalidRequestFailure(errors)));

        var requestId = Guid.NewGuid();
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
        await WeatherDataCollectionAggregate.Hydrate(eventPersistenceService, dataRejectedIntegrationEvent.StreamId)
            .Then(x => x.AppendModelingDataRejectedEvent(dataRejectedIntegrationEvent.Reason))
            .Then(contributorPaymentService.RevokePendingPayment)
            .ThrowOnFailure(nameof(ModelingDataRejectedIntegrationEvent));
    }

    public async Task HandleEvent(ModelingDataAcceptedIntegrationEvent dataAcceptedIntegrationEvent)
    {
        await WeatherDataCollectionAggregate.Hydrate(eventPersistenceService, dataAcceptedIntegrationEvent.StreamId)
            .Then(x => x.AppendModelingDataAcceptedEvent())
            .Then(contributorPaymentService.CommitPendingPayment)
            .Then(x => x.AppendSubmissionCompleteEvent())
            .ThrowOnFailure(nameof(ModelingDataAcceptedIntegrationEvent));
    }

    public async Task HandleEvent(ModelUpdatedIntegrationEvent integrationEvent)
    {
        await WeatherDataCollectionAggregate.Hydrate(eventPersistenceService, integrationEvent.StreamId)
            //.Then(x => x.AppendModelUpdatedEvent())
            .Then(eventPersistenceService.AppendModelUpdatedEventAndCreateOutboxItem) // Appends domain event and persists outbox item in single transaction.
            //.Then(notificationService.NotifyModelUpdated)
            .ThrowOnFailure(nameof(ModelUpdatedIntegrationEvent));
    }
}