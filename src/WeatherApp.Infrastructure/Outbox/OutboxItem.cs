using System.Text.Json;

namespace WeatherApp.Infrastructure.Outbox;

public record OutboxItem(
    long Id,
    string TypeName,
    string SerialisedData,
    string MessagingEntityName,
    DateTimeOffset Created
)
{
    public static OutboxItem Create<T>(T messageObject, string messagingEntityName)
    {
        var type = typeof(T);
        var typeName = type.FullName ?? type.Name;
        return new OutboxItem(
            OutboxConstants.NoIdYet,
            typeName,
            JsonSerializer.Serialize(messageObject),
            messagingEntityName,
            TimeProvider.System.GetUtcNow());
    }
}