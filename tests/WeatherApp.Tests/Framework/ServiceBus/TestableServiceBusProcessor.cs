using System.Text.Json;
using Azure.Messaging.ServiceBus;
using WeatherApp.Domain.EventSourcing;

namespace WeatherApp.Tests.Framework.ServiceBus;

public class TestableServiceBusProcessor(string entityName) : ServiceBusProcessor
{
    public string EntityName { get; } = entityName;
    public List<TestableProcessMessageEventArgs> MessageDeliveryAttempts = [];

    public async Task SendMessageWithRetries<T>(T request, int maxDeliveryCount = 10) where T : class
    {
        for (var attempt = 1; attempt <= maxDeliveryCount; attempt++)
        {
            if (attempt > 1)
            {
                var previousAttempt = MessageDeliveryAttempts.Last();
                if (previousAttempt.WasDeadLettered || previousAttempt.WasCompleted)
                    return;
            }

            await SendMessage(request, attempt);
        }
    }

    public async Task SendMessage<T>(T request, int deliveryCount = 1, Dictionary<string, object>? applicationProperties = null) where T : class
    {
        var args = CreateMessageArgs(request, deliveryCount, applicationProperties);
        MessageDeliveryAttempts.Add((TestableProcessMessageEventArgs)args);
        await base.OnProcessMessageAsync(args);
    }

    public async Task SendMessage(string json)
    {
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString(json));

        var args = new TestableProcessMessageEventArgs(message);

        MessageDeliveryAttempts.Add(args);
        await base.OnProcessMessageAsync(args);
    }

    public override Task StartProcessingAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public static ProcessMessageEventArgs CreateMessageArgs<T>(T payload, int deliveryCount = 1, Dictionary<string, object>? applicationProperties = null) where T : class
    {
        var payloadJson = JsonSerializer.Serialize(payload, GlobalJsonSerialiserSettings.Default);

        var correlationId = Guid.NewGuid().ToString();
        applicationProperties ??= new Dictionary<string, object>
        {
            { "origin", "ComponentTests" }
        };

        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString(payloadJson),
            correlationId: correlationId,
            properties: applicationProperties,
            deliveryCount: deliveryCount);

        var args = new TestableProcessMessageEventArgs(message);

        return args;
    }
}
