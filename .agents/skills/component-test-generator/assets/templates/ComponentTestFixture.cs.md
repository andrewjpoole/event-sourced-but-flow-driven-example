# ComponentTestFixture template

Purpose: centralise all shared test doubles and host factories for a single component test. Adapt the placeholders to the target solution by replacing app names, namespaces, repository interfaces, HTTP clients, Service Bus message types, and entity-name mapping logic.

```csharp
using Microsoft.Extensions.Time.Testing;
using Moq;
using {Namespace}.Infrastructure.Messaging;
using {Namespace}.Tests.TUnit.AppHostFactories;
using {Namespace}.Tests.TUnit.Framework;
using {Namespace}.Tests.TUnit.Framework.Persistence;
using {Namespace}.Tests.TUnit.Framework.ServiceBus;

namespace {Namespace}.Tests.TUnit;

public sealed class ComponentTestFixture : IDisposable
{
    private string _phase = string.Empty;

    // One WebApplicationFactory per hosted executable that participates in the scenario.
    public readonly {ApiApp}WebApplicationFactory ApiFactory;
    public readonly {EventListenerApp}WebApplicationFactory EventListenerFactory;
    public readonly {OutboxApp}WebApplicationFactory OutboxFactory;

    // Shared in-memory persistence. All hosts should resolve the same instances.
    public EventRepositoryInMemory EventRepositoryInMemory { get; } = new();
    public OutboxRepositoryInMemory OutboxRepositoryInMemory { get; } = new();

    // Real Kestrel server for all external HTTP dependencies.
    // Must be created BEFORE the app factories so its port is known for URL overrides.
    public FakeExternalServicesServer FakeExternalServicesServer { get; }

    // Fake Service Bus contains inbound processors and outbound senders.
    public FakeServiceBus FakeServiceBus { get; }

    // Fake time lets timer-driven workers advance immediately in tests.
    public FakeTimeProvider FakeTimeProvider { get; }

    public ComponentTestFixture()
    {
        // FakeExternalServicesServer MUST be created first so its port is available
        // when the app factories configure their URL overrides.
        FakeExternalServicesServer = new FakeExternalServicesServer();

        // Factories get the fixture so they can pull shared fakes from it.
        ApiFactory = new {ApiApp}WebApplicationFactory(this);
        EventListenerFactory = new {EventListenerApp}WebApplicationFactory(this);
        OutboxFactory = new {OutboxApp}WebApplicationFactory(this);

        // These lambdas must match the application's own entity-name mapping logic.
        FakeServiceBus = new FakeServiceBus(
            entityName => EntityNames.GetTypeNameFromEntityName(entityName),
            type => EntityNames.GetEntityNameFromTypeName(type));

        // Register every inbound integration event the app listens for.
        FakeServiceBus.AddProcessorFor<{InboundEvent1}>();
        FakeServiceBus.AddProcessorFor<{InboundEvent2}>();

        // Register every outbound integration event the app publishes.
        FakeServiceBus.AddSenderFor<{OutboundEvent1}>();
        FakeServiceBus.AddSenderFor<{OutboundEvent2}>();

        // Optional: useful when you want publish -> consume round-trips in one test.
        FakeServiceBus.MessagesSentToSendersWillBeReceivedOnCorrespondingProcessors();

        FakeTimeProvider = new FakeTimeProvider();
        FakeTimeProvider.SetUtcNow(TimeProvider.System.GetUtcNow());
        FakeTimeProvider.AutoAdvanceAmount = TimeSpan.FromMilliseconds(100);
    }

    public (Given given, When when, Then then, CannedData cannedData) SetupHelpers()
    {
        return (new Given(this), new When(this), new Then(this), new CannedData());
    }

    public void SetPhase(string newPhase) => _phase = newPhase;

    public string CurrentPhase => string.IsNullOrWhiteSpace(_phase)
        ? string.Empty
        : $"In phase {_phase}, ";

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        // Clean up any HttpClients created by the factories.
        ApiFactory.HttpClient?.Dispose();
        EventListenerFactory.HttpClient?.Dispose();
        OutboxFactory.HttpClient?.Dispose();

        // Dispose the fake server after the app factories to avoid connection errors during shutdown.
        FakeExternalServicesServer.Dispose();
    }
}
```

Adaptation notes:

- Remove `OutboxRepositoryInMemory` and `OutboxFactory` only if the target solution has no outbox worker.
- Register all inbound and outbound Service Bus message types used by the apps under test.
- Keep one fresh `ComponentTestFixture` per test for isolation.
- `FakeExternalServicesServer` must always be the first field initialised in the constructor.
