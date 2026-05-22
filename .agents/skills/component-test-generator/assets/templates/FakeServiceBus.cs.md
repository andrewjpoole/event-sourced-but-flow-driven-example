# FakeServiceBus template

Purpose: replace Azure Service Bus with in-memory processors and sender mocks that still exercise the app's real message handlers and publisher code. Adapt the entity-name mapping functions to match the target app exactly.

```csharp
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace {Namespace}.Tests.TUnit.Framework.ServiceBus;

public sealed class FakeServiceBus(
    Func<string, string> getTypeNameFromEntityName,
    Func<Type, string> getEntityNameFromType)
{
    private readonly Dictionary<Type, TestableServiceBusProcessor> _processors = new();
    private readonly Dictionary<Type, Mock<ServiceBusSender>> _mockSenders = new();

    public void AddProcessorFor<T>() where T : class
    {
        var processor = new TestableServiceBusProcessor(getEntityNameFromType(typeof(T)));
        _processors.Add(typeof(T), processor);
    }

    public void AddSenderFor<T>() where T : class
    {
        _mockSenders.Add(typeof(T), new Mock<ServiceBusSender>());
    }

    public bool HasProcessorFor<T>() where T : class => _processors.ContainsKey(typeof(T));

    public TestableServiceBusProcessor GetProcessorFor<T>() where T : class => _processors[typeof(T)];

    public TestableServiceBusProcessor GetProcessorFor(Type type) => _processors[type];

    public Mock<ServiceBusSender>? GetSenderFor<T>() where T : class
        => _mockSenders.TryGetValue(typeof(T), out var sender) ? sender : null;

    public void WireUpSendersAndProcessors(IServiceCollection services)
    {
        var mockClient = new Mock<ServiceBusClient>();

        mockClient
            .Setup(x => x.CreateProcessor(It.IsAny<string>(), It.IsAny<ServiceBusProcessorOptions>()))
            .Returns((string entityName, ServiceBusProcessorOptions _) => GetProcessorByQueueName(entityName));

        mockClient
            .Setup(x => x.CreateSender(It.IsAny<string>()))
            .Returns((string entityName) => GetSenderByQueueName(entityName));

        services.AddSingleton(mockClient.Object);
    }

    public TestableServiceBusProcessor GetProcessorByQueueName(string entityName)
    {
        // Some apps prefix entity names with the machine name; strip it if present.
        var machineName = Environment.MachineName.ToLowerInvariant();
        if (entityName.StartsWith(machineName, StringComparison.OrdinalIgnoreCase))
        {
            entityName = entityName.Replace($"{machineName}-", string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        var typeName = getTypeNameFromEntityName(entityName);

        foreach (var pair in _processors)
        {
            if (pair.Key.Name == typeName)
            {
                return pair.Value;
            }
        }

        throw new InvalidOperationException($"No TestableServiceBusProcessor registered for entity '{entityName}'.");
    }

    public ServiceBusSender GetSenderByQueueName(string entityName)
    {
        var machineName = Environment.MachineName.ToLowerInvariant();
        if (entityName.StartsWith(machineName, StringComparison.OrdinalIgnoreCase))
        {
            entityName = entityName.Replace($"{machineName}-", string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        var typeName = getTypeNameFromEntityName(entityName);

        foreach (var pair in _mockSenders)
        {
            if (pair.Key.Name == typeName)
            {
                return pair.Value.Object;
            }
        }

        throw new InvalidOperationException($"No ServiceBusSender registered for entity '{entityName}'.");
    }

    public void MessagesSentToSendersWillBeReceivedOnCorrespondingProcessors()
    {
        foreach (var senderPair in _mockSenders)
        {
            if (!_processors.ContainsKey(senderPair.Key))
            {
                continue;
            }

            senderPair.Value
                .Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
                .Callback<ServiceBusMessage, CancellationToken>((message, _) =>
                {
                    var payload = JsonSerializer.Deserialize(message.Body, senderPair.Key)
                        ?? throw new InvalidOperationException("Unable to deserialise Service Bus payload.");

                    var properties = (Dictionary<string, object>?)message.ApplicationProperties;
                    GetProcessorFor(senderPair.Key)
                        .PresentMessage(payload, applicationProperties: properties)
                        .GetAwaiter()
                        .GetResult();
                });
        }
    }

    public void ClearDeliveryAttemptsOnAllProcessors()
    {
        foreach (var processor in _processors.Values)
        {
            processor.MessageDeliveryAttempts.Clear();
        }
    }

    public void ClearInvocationsOnAllSenders()
    {
        foreach (var sender in _mockSenders.Values)
        {
            sender.Invocations.Clear();
        }
    }
}
```

Important note:

The two mapping lambdas must match the app's production mapping logic exactly. If the app uses an `EntityNames` class, service bus options, or environment-variable-driven entity names, mirror that logic here so processor and sender lookups succeed.
