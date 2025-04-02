namespace WeatherApp.Infrastructure.Outbox;

public interface IOutboxBatchRepository
{
    Task<IEnumerable<OutboxBatchItem>> GetNextBatchAsync(int batchSize);    
}