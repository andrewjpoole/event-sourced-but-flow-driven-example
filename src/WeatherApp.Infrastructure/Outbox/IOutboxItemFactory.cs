namespace WeatherApp.Infrastructure.Outbox;

public interface IOutboxItemFactory
{
    OutboxItem Create<T>(T messageObject, string? messagingEntityName = null);
}
