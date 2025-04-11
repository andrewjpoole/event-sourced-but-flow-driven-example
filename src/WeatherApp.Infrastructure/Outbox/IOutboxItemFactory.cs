namespace WeatherApp.Infrastructure.Outbox;

public interface IOutboxItemFactory
{
    OutboxItem Create<T>(T messageObject, string associatedId, string? messagingEntityName = null);
}
