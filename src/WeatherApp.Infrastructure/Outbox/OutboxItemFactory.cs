using System.Text.Json;
using Microsoft.Extensions.Options;
using WeatherApp.Infrastructure.Messaging;

namespace WeatherApp.Infrastructure.Outbox;

public class OutboxItemFactory(IOptions<ServiceBusOutboundOptions> options) : IOutboxItemFactory
{
    private readonly ServiceBusOutboundOptions serviceBusOptions = options.Value;

    public OutboxItem Create<T>(T messageObject, string associatedId, string? messagingEntityName = null)
    {
        var type = typeof(T);
        if(messagingEntityName == null)
        {
            var entityNameForTypeFromConfig = serviceBusOptions.ResolveQueueOrTopicNameFromConfig(type.Name);
            messagingEntityName = new QueueOrTopicName(entityNameForTypeFromConfig).Name;
        }        

        return new OutboxItem(
            OutboxConstants.NoIdYet,
            associatedId,
            type.Name,
            JsonSerializer.Serialize(messageObject),
            messagingEntityName,
            TimeProvider.System.GetUtcNow());
    }
}