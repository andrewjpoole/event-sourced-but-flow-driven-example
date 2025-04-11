namespace WeatherApp.Infrastructure.Outbox;

public record OutboxItem(
    long Id,
    string AssociatedId,
    string TypeName,
    string SerialisedData,
    string MessagingEntityName,
    DateTimeOffset Created
);
