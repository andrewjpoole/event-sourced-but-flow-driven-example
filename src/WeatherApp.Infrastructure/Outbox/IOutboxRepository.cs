using WeatherApp.Infrastructure.RetryableDapperConnection;

namespace WeatherApp.Infrastructure.Outbox;

public interface IOutboxRepository
{
    Task<long> Add(OutboxItem outboxItem, IDbTransactionWrapped? transaction = null);
    Task AddScheduled(OutboxItem outboxItem, DateTimeOffset retryAfter);
    Task AddSentStatus(OutboxSentStatusUpdate outboxSentStatusUpdate, IDbTransactionWrapped? transaction = null);
    /// <summary>
    /// Remove outbox items whose latest status is Sent and whose Sent status was created before the provided cutoff.
    /// Returns the number of outbox items removed.
    /// </summary>
    Task<int> RemoveSentOutboxItemsOlderThan(DateTimeOffset cutoff);
}
