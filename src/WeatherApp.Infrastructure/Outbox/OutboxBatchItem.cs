using System.Text.Json.Serialization;

namespace WeatherApp.Infrastructure.Outbox;

public record OutboxBatchItem
(
    long Id,
    string TypeName,
    string SerialisedData,
    string MessagingEntityName,
    DateTimeOffset Created,
    OutboxSentStatus Status,
    DateTimeOffset? NotBefore
    // ToDo add number of previous attempts to send the message for exponential backoff
)
{
    [JsonConstructor]
    public OutboxBatchItem(long id, string typeName, string serialisedData, string messagingEntityName, DateTimeOffset created, byte status, DateTimeOffset? notBefore)
        : this(id, typeName, serialisedData, messagingEntityName, created, (OutboxSentStatus)status, notBefore)
    {
    }
}
