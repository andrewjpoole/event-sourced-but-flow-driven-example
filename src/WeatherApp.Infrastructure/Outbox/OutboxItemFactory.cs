using System.Text.Json;
using Microsoft.Extensions.Options;
using WeatherApp.Infrastructure.MessageBus;

namespace WeatherApp.Infrastructure.Outbox;

public class OutboxItemFactory(IOptions<ServiceBusOutboundEntityOptions> options) : IOutboxItemFactory
{
    private readonly ServiceBusOutboundEntityOptions serviceBusOptions = options.Value;

    public OutboxItem Create<T>(T messageObject, string? messagingEntityName = null)
    {
        var type = typeof(T);
        if(messagingEntityName == null)
        {
            var entityNameForTypeFromConfig = serviceBusOptions.ResolveQueueOrTopicNameFromConfig(type.Name);
            messagingEntityName = new QueueOrTopicName(entityNameForTypeFromConfig).Name;
        }        

        return new OutboxItem(
            OutboxConstants.NoIdYet,
            type.Name,
            JsonSerializer.Serialize(messageObject),
            messagingEntityName,
            TimeProvider.System.GetUtcNow());
    }
}