using System.Collections.ObjectModel;
using System.Text.Json;
using Azure.Messaging.ServiceBus;

namespace WeatherApp.Infrastructure.MessageBus;

public class MessageSender<T> : IMessageSender<T>
{
    private readonly ServiceBusSender serviceBusSender;

    public MessageSender(ServiceBusClient serviceBusClient, ServiceBusOutboundEntitiyOptions options)
    {
        var type = typeof(T);
        var typeName = type.FullName ?? type.Name;
        var entityName = options.ResolveQueueOrTopicNameFromConfig(typeName);

        serviceBusSender = serviceBusClient.CreateSender(entityName);
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
