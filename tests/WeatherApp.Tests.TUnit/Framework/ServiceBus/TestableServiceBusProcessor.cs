using System.Text.Json;
using Azure.Messaging.ServiceBus;
using WeatherApp.Domain.EventSourcing;

namespace WeatherApp.Tests.TUnit.Framework.ServiceBus;

public class TestableServiceBusProcessor(string entityName) : ServiceBusProcessor
{
    public List<TestableMessageEventArgs> MessageDeliveryAttempts = [];
    
    public string EntityName { get; } = entityName;

    public async Task PresentMessageWithRetries<T>(T message, int maxDeliveryCount = 10) where T : class
    {
        for (var attempt = 1; attempt <= maxDeliveryCount; attempt++)
        {
            if (attempt > 1)
            {
                var previousAttempt = MessageDeliveryAttempts.Last();
                if (previousAttempt.WasDeadLettered || previousAttempt.WasCompleted)
                    return;
            }

            await PresentMessage(message, attempt);
        }
    }

    public async Task PresentMessage<T>(T message, int deliveryCount = 1, Dictionary<string, object>? applicationProperties = null) where T : class
    {
        var args = CreateMessageArgs(message, deliveryCount, applicationProperties);
        MessageDeliveryAttempts.Add((TestableMessageEventArgs)args);
        await base.OnProcessMessageAsync(args);
    }

    public async Task PresentMessage(string json)
    {
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString(json));

        var args = new TestableMessageEventArgs(message);

        MessageDeliveryAttempts.Add(args);
        await base.OnProcessMessageAsync(args);
    }

    public override Task StartProcessingAsync(CancellationToken cancellationToken = default) 
        => Task.CompletedTask;

    public static ProcessMessageEventArgs CreateMessageArgs<T>(T payload, int deliveryCount = 1, Dictionary<string, object>? applicationProperties = null) where T : class
    {
        var payloadJson = JsonSerializer.Serialize(payload, GlobalJsonSerialiserSettings.Default);

        var correlationId = Guid.NewGuid().ToString();
        applicationProperties ??= new Dictionary<string, object>
        {
            { "origin", "ComponentTests" }
        };

        var message = ServiceBusModelFactory
            .ServiceBusReceivedMessage(
                body: BinaryData.FromString(payloadJson),
                correlationId: correlationId,
                properties: applicationProperties,
                deliveryCount: deliveryCount);

        return new TestableMessageEventArgs(message);
    }
}
