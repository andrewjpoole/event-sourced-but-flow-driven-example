using System.Collections.Concurrent;
using System.Text.Json;
using Azure.Messaging.ServiceBus;

namespace WeatherApp.Infrastructure.MessageBus;

public class UniversalMessageSender : IUniversalMessageSender
{
    private readonly ServiceBusClient serviceBusClient;
    private readonly ConcurrentDictionary<string, ServiceBusSender> senders = new();

    public UniversalMessageSender(ServiceBusClient serviceBusClient)
    {
        this.serviceBusClient = serviceBusClient;
    }

    public async Task SendAsync(object payload, string entityName, CancellationToken cancellationToken = default)
    {
        var messageBody = JsonSerializer.Serialize(payload);
        await SendAsync(messageBody, entityName, cancellationToken);
    }

    public Task SendAsync(string serializedPayload, string entityName, CancellationToken cancellationToken = default)
    {
        var message = new ServiceBusMessage(serializedPayload)
        {
            ApplicationProperties =
            {
                ["MessageType"] = "string",
                ["MessageId"] = Guid.NewGuid().ToString(),
                ["CorrelationId"] = Guid.NewGuid().ToString()
            }
        };
        var sender = senders.GetOrAdd(entityName, serviceBusClient.CreateSender(entityName));
        return sender.SendMessageAsync(message, cancellationToken);
    }
}
