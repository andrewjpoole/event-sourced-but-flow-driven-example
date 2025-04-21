using Microsoft.Extensions.Time.Testing;
using Moq;
using WeatherApp.Application.Models.IntegrationEvents.NotificationEvents;
using WeatherApp.Application.Models.IntegrationEvents.WeatherModelingEvents;
using WeatherApp.Infrastructure.Messaging;
using WeatherApp.Tests.AppHostFactories;
using WeatherApp.Tests.e2eComponentTests.Framework.Persistence;
using WeatherApp.Tests.Framework;
using WeatherApp.Tests.Framework.ServiceBus;

namespace WeatherApp.Tests;

public class ComponentTestFixture : IDisposable
{
    private string phase = "";

    public readonly ApiWebApplicationFactory ApiFactory;
    public readonly EventListenerWebApplicationFactory EventListenerFactory;
    public readonly OutboxApplicationFactory OutboxApplicationFactory;
    public readonly NotificationServiceWebApplicationFactory NotificationServiceFactory;

    public readonly FakeTimeProvider FakeTimeProvider;    
    public readonly FakeServiceBus FakeServiceBus;
    public EventRepositoryInMemory EventRepositoryInMemory = new();
    public OutboxRepositoryInMemory OutboxRepositoryInMemory = new();

    public readonly Mock<HttpMessageHandler> MockContributorPaymentsServiceHttpMessageHandler = new(MockBehavior.Strict);

    public ComponentTestFixture()
    {
        ApiFactory = new(this) { SetSharedEventRepository = () => EventRepositoryInMemory };
        EventListenerFactory = new(this) 
        { 
            SetSharedEventRepository = () => EventRepositoryInMemory,
            SetSharedOutboxRepositories = () => OutboxRepositoryInMemory
        };
        OutboxApplicationFactory = new(this) { SetSharedOutboxRepositories = () => OutboxRepositoryInMemory };
        NotificationServiceFactory = new(this);

        FakeServiceBus = new FakeServiceBus(
            entityName => EntityNames.GetTypeNameFromEntityName(entityName), 
            type => EntityNames.GetEntityNameFromTypeName(type));

        FakeServiceBus.AddSenderFor<UserNotificationEvent>();
        FakeServiceBus.AddProcessorFor<ModelingDataAcceptedIntegrationEvent>();
        FakeServiceBus.AddProcessorFor<ModelingDataRejectedIntegrationEvent>();
        FakeServiceBus.AddProcessorFor<ModelUpdatedIntegrationEvent>();
        FakeServiceBus.AddProcessorFor<UserNotificationEvent>();

        FakeServiceBus.MessagesSentToSendersWillBeReceivedOnCorrespondingProcessors();
        
        FakeTimeProvider = new FakeTimeProvider();
        FakeTimeProvider.SetUtcNow(TimeProvider.System.GetUtcNow());
        FakeTimeProvider.AutoAdvanceAmount = TimeSpan.FromMilliseconds(100);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        ApiFactory.HttpClient?.Dispose();
        EventListenerFactory.HttpClient?.Dispose();
        NotificationServiceFactory.HttpClient?.Dispose();
    }

    public (Given given, When when, Then then) SetupHelpers()
    {
        return (new Given(this), new When(this), new Then(this));
    }

    public void SetPhase(string newPhase) => phase = newPhase;
    public string CurrentPhase => string.IsNullOrWhiteSpace(phase) ? string.Empty : $"In phase {phase}, ";
}