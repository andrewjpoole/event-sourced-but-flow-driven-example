using System.Text.Json.Serialization;

namespace WeatherApp.Infrastructure.Outbox;

public record OutboxBatchItem
(
    long Id,
    string AssociatedId,
    string TypeName,
    string SerialisedData,
    string MessagingEntityName,
    string SerialisedTelemetry,
    DateTimeOffset Created,
    OutboxSentStatus Status,
    DateTimeOffset? NotBefore
    // ToDo add number of previous attempts to send the message for exponential backoff
)
{
    [JsonConstructor]
    public OutboxBatchItem(long id, string associatedId, string typeName, string serialisedData, string messagingEntityName, string serialisedTelemetry, DateTimeOffset created, byte status, DateTimeOffset? notBefore)
        : this(id, associatedId, typeName, serialisedData, messagingEntityName, serialisedTelemetry, created, (OutboxSentStatus)status, notBefore)
    {
    }
}
