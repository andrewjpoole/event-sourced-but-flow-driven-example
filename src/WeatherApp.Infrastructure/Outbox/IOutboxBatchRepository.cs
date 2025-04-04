using WeatherApp.Infrastructure.RetryableDapperConnection;

namespace WeatherApp.Infrastructure.Outbox;

public interface IOutboxBatchRepository
{
    Task<IEnumerable<OutboxBatchItem>> GetNextBatchAsync(int batchSize, IDbTransactionWrapped dbTransactionWrapped);    
}