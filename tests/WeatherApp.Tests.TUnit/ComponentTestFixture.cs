using Microsoft.Extensions.Time.Testing;
using Moq;
using WeatherApp.Application.Models.IntegrationEvents.NotificationEvents;
using WeatherApp.Application.Models.IntegrationEvents.WeatherModelingEvents;
using WeatherApp.Infrastructure.Messaging;
using WeatherApp.Tests.TUnit.AppHostFactories;
using WeatherApp.Tests.TUnit.Framework.Persistence;
using WeatherApp.Tests.TUnit.Framework;
using WeatherApp.Tests.TUnit.Framework.ServiceBus;

namespace WeatherApp.Tests.TUnit;

public class ComponentTestFixture : IDisposable
{
    private string phase = "";

    // Application Host Factories for the 3 executables.
    public readonly ApiWebApplicationFactory ApiFactory;
    public readonly EventListenerWebApplicationFactory EventListenerFactory;
    public readonly OutboxApplicationFactory OutboxApplicationFactory;

    // Mocked or Faked external dependencies.
    public EventRepositoryInMemory EventRepositoryInMemory = new();
    public OutboxRepositoryInMemory OutboxRepositoryInMemory = new();
    
    public readonly Mock<HttpMessageHandler> MockContributorPaymentsServiceHttpMessageHandler = 
        new(MockBehavior.Strict);

    public readonly Mock<HttpMessageHandler> MockWeatherModelingServiceHttpMessageHandler = 
        new(MockBehavior.Strict);
    
    public readonly FakeServiceBus FakeServiceBus;
    public readonly FakeTimeProvider FakeTimeProvider;    

    public ComponentTestFixture()
    {
        ApiFactory = new(this);
        EventListenerFactory = new(this);
        OutboxApplicationFactory = new(this);

        FakeServiceBus = new FakeServiceBus(
            entityName => EntityNames.GetTypeNameFromEntityName(entityName), 
            type => EntityNames.GetEntityNameFromTypeName(type));

        FakeServiceBus.AddProcessorFor<ModelingDataAcceptedIntegrationEvent>();
        FakeServiceBus.AddProcessorFor<ModelingDataRejectedIntegrationEvent>();
        FakeServiceBus.AddProcessorFor<ModelUpdatedIntegrationEvent>();
        FakeServiceBus.AddSenderFor<UserNotificationEvent>();

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
        OutboxApplicationFactory.HttpClient?.Dispose();
    }

    public (Given given, When when, Then then, CannedData cannedData) SetupHelpers()
    {
        return (new Given(this), new When(this), new Then(this), new CannedData());
    }

    public void SetPhase(string newPhase) => phase = newPhase;
    public string CurrentPhase => string.IsNullOrWhiteSpace(phase) ? string.Empty : $"In phase {phase}, ";
}