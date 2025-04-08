using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;

namespace WeatherApp.Infrastructure.MessageBus;

public class MessageSender<T> : IMessageSender<T>
{
    private readonly ServiceBusSender serviceBusSender;

    public MessageSender(ServiceBusClient serviceBusClient, IOptions<ServiceBusOutboundEntityOptions> options)
    {
        var type = typeof(T);
        var entityNameFotTypeFromConfig = options.Value.ResolveQueueOrTopicNameFromConfig(type.Name);
        var queueOrTopicName = new QueueOrTopicName(entityNameFotTypeFromConfig).Name;

        serviceBusSender = serviceBusClient.CreateSender(queueOrTopicName);
    }

    public async Task SendAsync(T payload, CancellationToken cancellationToken = default)
    {
        var messageBody = JsonSerializer.Serialize(payload);
        var message = new ServiceBusMessage(messageBody)
        {
            ApplicationProperties =
            {
                ["MessageType"] = typeof(T).FullName,
                ["MessageId"] = Guid.NewGuid().ToString(),
                ["CorrelationId"] = Guid.NewGuid().ToString()
            }
        };
        await serviceBusSender.SendMessageAsync(message, cancellationToken);
    }
}
