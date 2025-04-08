namespace WeatherApp.Infrastructure.Outbox;

public record OutboxItem(
    long Id,
    string TypeName,
    string SerialisedData,
    string MessagingEntityName,
    DateTimeOffset Created
);
