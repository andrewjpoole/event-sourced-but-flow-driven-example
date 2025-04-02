using System.Data;

namespace WeatherApp.Infrastructure.Outbox;

public interface IOutboxRepository
{
    Task<long> Add(OutboxItem outboxItem, IDbTransaction? transaction = null);
    Task<long> AddScheduled(OutboxItem outboxItem, DateTimeOffset retryAfter);
    Task<long> AddSentStatus(OutboxSentStatusUpdate outboxSentStatusUpdate, IDbTransaction? transaction = null);
}
