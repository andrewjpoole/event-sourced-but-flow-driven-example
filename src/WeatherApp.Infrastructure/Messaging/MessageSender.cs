using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;

namespace WeatherApp.Infrastructure.Messaging;

public class MessageSender<T> : IMessageSender<T>
{
    private readonly ServiceBusSender serviceBusSender;

    public MessageSender(ServiceBusClient serviceBusClient, IOptions<ServiceBusOutboundOptions> options)
    {
        var type = typeof(T);
        var entityNameForTypeFromConfig = options.Value.ResolveQueueOrTopicNameFromConfig(type.Name);
        var queueOrTopicName = new QueueOrTopicName(entityNameForTypeFromConfig).Name;

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
                ["MessageId"] = Guid.NewGuid().ToString()
            }
        };
        await serviceBusSender.SendMessageAsync(message, cancellationToken);
    }
}
