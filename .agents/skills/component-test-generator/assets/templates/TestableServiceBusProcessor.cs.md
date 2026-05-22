# TestableServiceBusProcessor template

Purpose: provide a controllable `ServiceBusProcessor` implementation that lets tests inject messages directly into the app's real Service Bus handlers. Adapt JSON settings and helper extensions to the target solution.

```csharp
using System.Text.Json;
using Azure.Messaging.ServiceBus;

namespace {Namespace}.Tests.TUnit.Framework.ServiceBus;

public sealed class TestableServiceBusProcessor(string entityName) : ServiceBusProcessor
{
    public string EntityName { get; } = entityName;

    public List<TestableMessageEventArgs> MessageDeliveryAttempts { get; } = [];

    public async Task PresentMessage<T>(
        T message,
        int deliveryCount = 1,
        Dictionary<string, object>? applicationProperties = null) where T : class
    {
        var args = CreateMessageArgs(message, deliveryCount, applicationProperties);
        MessageDeliveryAttempts.Add((TestableMessageEventArgs)args);

        // Calls the same registered handler the real processor would use.
        await base.OnProcessMessageAsync(args);
    }

    public async Task PresentMessageWithRetries<T>(T message, int maxDeliveryCount = 10) where T : class
    {
        for (var attempt = 1; attempt <= maxDeliveryCount; attempt++)
        {
            if (attempt > 1)
            {
                var previousAttempt = MessageDeliveryAttempts.Last();
                if (previousAttempt.WasCompleted || previousAttempt.WasDeadLettered)
                {
                    return;
                }
            }

            await PresentMessage(message, attempt);
        }
    }

    public static ProcessMessageEventArgs CreateMessageArgs<T>(
        T payload,
        int deliveryCount = 1,
        Dictionary<string, object>? applicationProperties = null) where T : class
    {
        var payloadJson = JsonSerializer.Serialize(payload, GlobalJsonSerialiserSettings.Default);
        var correlationId = Guid.NewGuid().ToString();

        applicationProperties ??= new Dictionary<string, object>
        {
            ["origin"] = "ComponentTests"
        };

        var receivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString(payloadJson),
            correlationId: correlationId,
            properties: applicationProperties,
            deliveryCount: deliveryCount);

        return new TestableMessageEventArgs(receivedMessage);
    }

    public override Task StartProcessingAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

public sealed class TestableMessageEventArgs : ProcessMessageEventArgs
{
    public bool WasCompleted { get; private set; }
    public bool WasDeadLettered { get; private set; }

    public TestableMessageEventArgs(ServiceBusReceivedMessage message)
        : base(message, receiver: null!, CancellationToken.None)
    {
    }

    public override Task CompleteMessageAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default)
    {
        WasCompleted = true;
        return Task.CompletedTask;
    }

    public override Task DeadLetterMessageAsync(
        ServiceBusReceivedMessage message,
        IDictionary<string, object>? propertiesToModify = null,
        CancellationToken cancellationToken = default)
    {
        WasDeadLettered = true;
        return Task.CompletedTask;
    }
}
```

Short `TestableMessageEventArgs.cs` snippet if you prefer a separate file:

```csharp
public sealed class TestableMessageEventArgs : ProcessMessageEventArgs
{
    public bool WasCompleted { get; private set; }
    public bool WasDeadLettered { get; private set; }

    public TestableMessageEventArgs(ServiceBusReceivedMessage message)
        : base(message, receiver: null!, CancellationToken.None)
    {
    }

    public override Task CompleteMessageAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default)
    {
        WasCompleted = true;
        return Task.CompletedTask;
    }

    public override Task DeadLetterMessageAsync(ServiceBusReceivedMessage message, IDictionary<string, object>? propertiesToModify = null, CancellationToken cancellationToken = default)
    {
        WasDeadLettered = true;
        return Task.CompletedTask;
    }
}
```
