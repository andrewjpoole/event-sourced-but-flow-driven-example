using WeatherApp.Application.Models.IntegrationEvents.WeatherModelingEvents;
using WeatherApp.Application.Services;
using WeatherApp.Domain.Outcomes;
using WeatherApp.Application.Models.Requests;
using WeatherApp.Domain.DomainEvents;
using WeatherApp.Domain.Entities;
using WeatherApp.Domain.EventSourcing;
using WeatherApp.Domain.ServiceDefinitions;

namespace WeatherApp.Application.Orchestration;

public class CollectedWeatherDataOrchestrator : 
    IPostWeatherDataHandler,
    IEventHandler<ModelingDataAcceptedIntegrationEvent>, 
    IEventHandler<ModelingDataRejectedIntegrationEvent>,
    IEventHandler<ModelUpdatedIntegrationEvent>
{
    private readonly IEventPersistenceService eventPersistenceService;
    private readonly IWeatherModelingService weatherModelingService;
    private readonly INotificationService notificationService;
    private readonly IContributorPaymentService contributorPaymentService;

    public CollectedWeatherDataOrchestrator(
        IEventPersistenceService eventPersistenceService,
        IWeatherModelingService weatherModelingService,
        INotificationService notificationService, IContributorPaymentService contributorPaymentService)
    {
        this.eventPersistenceService = eventPersistenceService;
        this.weatherModelingService = weatherModelingService;
        this.notificationService = notificationService;
        this.contributorPaymentService = contributorPaymentService;
    }

    public Task<OneOf<WeatherDataCollectionResponse, Failure>> HandlePostWeatherData(string weatherDataLocation, 
        CollectedWeatherDataModel weatherDataModel, 
        IWeatherDataValidator weatherDataValidator, 
        ILocationManager locationManager)
    {
        if (weatherDataValidator.Validate(weatherDataModel, out var errors) == false)
            return Task.FromResult(OneOf<WeatherDataCollectionResponse, Failure>
                .FromT1(new InvalidRequestFailure(errors)));

        var requestId = Guid.NewGuid();
        return WeatherDataCollection.PersistOrHydrate(eventPersistenceService, requestId, 
                Event.Create(new WeatherDataCollectionInitiated(weatherDataModel.ToEntity(), weatherDataLocation), requestId))
            .Then(locationManager.Locate)
            .Then(contributorPaymentService.CreatePendingPayment)
            .Then(weatherModelingService.Submit, // call to service, async response via integration event 
                (c, f) => contributorPaymentService.RevokePendingPayment(c)) 
            .ToResult(WeatherDataCollectionResponse.FromWeatherDataCollection);
    }
    
    public async Task HandleEvent(ModelingDataRejectedIntegrationEvent dataRejectedIntegrationEvent)
    {
        await WeatherDataCollection.Hydrate(eventPersistenceService, dataRejectedIntegrationEvent.RequestId)
            .Then(x => x.AppendModelingDataRejectedEvent(dataRejectedIntegrationEvent.Reason))
            .Then(contributorPaymentService.RevokePendingPayment)
            .ThrowOnFailure(nameof(ModelingDataRejectedIntegrationEvent));
    }

    public async Task HandleEvent(ModelingDataAcceptedIntegrationEvent dataAcceptedIntegrationEvent)
    {
        await WeatherDataCollection.Hydrate(eventPersistenceService, dataAcceptedIntegrationEvent.RequestId)
            .Then(x => x.AppendModelingDataAcceptedEvent())
            .Then(contributorPaymentService.CommitPendingPayment)
            .Then(x => x.AppendSubmissionCompleteEvent())
            .ThrowOnFailure(nameof(ModelingDataAcceptedIntegrationEvent));
    }

    public async Task HandleEvent(ModelUpdatedIntegrationEvent integrationEvent)
    {
        await WeatherDataCollection.Hydrate(eventPersistenceService, integrationEvent.RequestId)
            .Then(x => x.AppendModelUpdatedEvent())
            .Then(notificationService.NotifyModelUpdated)
            .ThrowOnFailure(nameof(ModelUpdatedIntegrationEvent));
    }
}