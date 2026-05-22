# Given template

Purpose: hold readable precondition helpers for the component-test DSL. Adapt external service names, seeded events, and any domain-specific helper methods for the target app.

```csharp
using {Namespace}.Domain.EventSourcing;

namespace {Namespace}.Tests.TUnit.Framework;

public sealed class Given(ComponentTestFixture fixture)
{
    public Given And => this;

    public Given TheServersAreStarted()
    {
        // Start every hosted app that participates in the test.
        fixture.ApiFactory.Start();
        fixture.EventListenerFactory.Start();
        fixture.OutboxFactory.Start();
        return this;
    }

    public Given WeHaveResetEverything()
    {
        // Reset fakes so each scenario starts cleanly.
        fixture.FakeServiceBus.ClearDeliveryAttemptsOnAllProcessors();
        fixture.FakeServiceBus.ClearInvocationsOnAllSenders();
        fixture.FakeExternalServicesServer.State.Reset();
        fixture.EventRepositoryInMemory.PersistedEvents.Clear();
        fixture.OutboxRepositoryInMemory.OutboxItems.Clear();
        return this;
    }

    public Given ThereIsExistingData(List<Event> events)
    {
        fixture.EventRepositoryInMemory.InsertExistingEvents(events, fixture.FakeTimeProvider);
        Console.WriteLine($"[Given] Seeded {events.Count} event(s) into store: {string.Join(", ", events.Select(e => e.GetType().Name))}");
        return this;
    }

    // External service state is set directly on FakeServicesState — no mock setup needed.
    // The fake server reads these flags on each request.

    public Given The{ExternalService1}WillSucceed()
    {
        fixture.FakeExternalServicesServer.State.{ExternalService1Accepted} = true;
        Console.WriteLine("[Given] {ExternalService1} → will return 200");
        return this;
    }

    public Given The{ExternalService1}WillFail()
    {
        fixture.FakeExternalServicesServer.State.{ExternalService1Accepted} = false;
        Console.WriteLine("[Given] {ExternalService1} → will return error");
        return this;
    }

    public Given MessagesSentWillBeReceived<TMessageType>() where TMessageType : class
    {
        // This is useful when a message published by one app should immediately feed another app.
        if (!fixture.FakeServiceBus.HasProcessorFor<TMessageType>())
            return this;

        var senderMock = fixture.FakeServiceBus.GetSenderFor<TMessageType>();
        if (senderMock is null)
            return this;

        senderMock
            .Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((serviceBusMessage, _) =>
            {
                var message = serviceBusMessage.Body.ToObjectFromJson<TMessageType>();
                if (message is null)
                    throw new InvalidOperationException($"Unable to deserialise {typeof(TMessageType).Name} from Service Bus body.");

                var applicationProperties = (Dictionary<string, object>?)serviceBusMessage.ApplicationProperties;
                fixture.FakeServiceBus
                    .GetProcessorFor<TMessageType>()
                    .PresentMessage(message, applicationProperties: applicationProperties)
                    .GetAwaiter()
                    .GetResult();
            });

        return this;
    }
}
```

Notes:

- External service behaviour is controlled via `FakeServicesState` boolean flags. The `FakeExternalServicesServer` endpoint handlers read those flags on each inbound request — no mock setup, no URL predicate matching.
- Log each precondition with a `[Given]` prefix so the test output tells a story.
- Call `Console.WriteLine` from `Start()` in each factory so the bound address is visible per test (see `AppHostFactory.cs.md`).
