using WeatherApp.Application.Models.IntegrationEvents.WeatherModelingEvents;
using WeatherApp.Infrastructure.Persistence;
using WeatherApp.Tests.AppHostFactories;
using WeatherApp.Tests.Framework;
using WeatherApp.Tests.Framework.ServiceBus;

namespace WeatherApp.Tests;

public class ComponentTestFixture : IDisposable
{
    private string phase = "";

    public readonly ApiWebApplicationFactory ApiFactory;
    public readonly EventListenerWebApplicationFactory EventListenerFactory;
    public readonly NotificationServiceWebApplicationFactory NotificationServiceFactory;

    public readonly MockServiceBusSenderCollection MockServiceBusSenders;

    public EventRepositoryInMemory EventRepositoryInMemory = new();

    public ComponentTestFixture()
    {
        ApiFactory = new() { SetSharedEventRepository = () => EventRepositoryInMemory };
        EventListenerFactory = new() { SetSharedEventRepository = () => EventRepositoryInMemory };
        NotificationServiceFactory = new();

        MockServiceBusSenders = new MockServiceBusSenderCollection();
        MockServiceBusSenders.AddSenderFor<DummyIntegrationEvent>();
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