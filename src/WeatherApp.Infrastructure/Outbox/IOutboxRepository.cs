using WeatherApp.Infrastructure.RetryableDapperConnection;

namespace WeatherApp.Infrastructure.Outbox;

public interface IOutboxRepository
{
    Task<long> Add(OutboxItem outboxItem, IDbTransactionWrapped? transaction = null);
    Task AddScheduled(OutboxItem outboxItem, DateTimeOffset retryAfter);
    Task AddSentStatus(OutboxSentStatusUpdate outboxSentStatusUpdate, IDbTransactionWrapped? transaction = null);
}
