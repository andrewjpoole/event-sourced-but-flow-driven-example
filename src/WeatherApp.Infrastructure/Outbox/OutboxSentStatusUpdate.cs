namespace WeatherApp.Infrastructure.Outbox;

public record OutboxSentStatusUpdate(
    long Id,
    long OutboxItemId,
    OutboxSentStatus Status,
    DateTimeOffset? NotBefore,
    DateTimeOffset Created
)
{
    public static OutboxSentStatusUpdate CreateSent(long outboxItemId) =>
        new(OutboxConstants.NoIdYet, outboxItemId, OutboxSentStatus.Sent, null, TimeProvider.System.GetUtcNow());

    public static OutboxSentStatusUpdate CreateTransientFailure(long outboxItemId, DateTimeOffset notBefore) =>
        new(OutboxConstants.NoIdYet, outboxItemId, OutboxSentStatus.TransientFailure, notBefore, TimeProvider.System.GetUtcNow());

    public static OutboxSentStatusUpdate CreateScheduled(long outboxItemId, DateTimeOffset notBefore) =>
        new(OutboxConstants.NoIdYet, outboxItemId, OutboxSentStatus.Scheduled, notBefore, TimeProvider.System.GetUtcNow());

    public static OutboxSentStatusUpdate CreateCancelled(long outboxItemId) =>
        new(OutboxConstants.NoIdYet, outboxItemId, OutboxSentStatus.Cancelled, null, TimeProvider.System.GetUtcNow());

}