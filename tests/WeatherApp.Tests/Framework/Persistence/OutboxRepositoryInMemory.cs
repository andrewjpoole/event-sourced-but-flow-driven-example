using WeatherApp.Infrastructure.Outbox;
using WeatherApp.Infrastructure.RetryableDapperConnection;

namespace WeatherApp.Tests.e2eComponentTests.Framework.Persistence;

public class OutboxRepositoryInMemory : IOutboxRepository, IOutboxBatchRepository
{
    public Dictionary<long, OutboxItemWithSentStatuses> OutboxItems { get; } = new();
    
    private long GetNextId() => OutboxItems.Count == 0 ? Random.Shared.Next(1, 20_000) : OutboxItems.Keys.Max() + 1;

    public Task<long> Add(OutboxItem outboxItem, IDbTransactionWrapped? transaction = null)
    {
        var id = GetNextId();
        var newOutboxItem = new OutboxItemWithSentStatuses(outboxItem) { OutboxItem = outboxItem with { Id = id } };
        OutboxItems.Add(id, newOutboxItem);
        return Task.FromResult((long)id);
    }

    public Task AddScheduled(OutboxItem outboxItem, DateTimeOffset retryAfter)
    {
        var id = GetNextId();
        var newOutboxItem = new OutboxItemWithSentStatuses(outboxItem) { OutboxItem = outboxItem with { Id = id } };
        newOutboxItem.StatusUpdates.Add(OutboxSentStatusUpdate.CreateScheduled(id, retryAfter));
        OutboxItems.Add(id, newOutboxItem);
        return Task.FromResult((long)id);
    }

    public Task AddSentStatus(OutboxSentStatusUpdate outboxSentStatusUpdate, IDbTransactionWrapped? transaction = null)
    {
        if(OutboxItems.TryGetValue(outboxSentStatusUpdate.OutboxItemId, out var outboxItem))
        {
            outboxItem.StatusUpdates.Add(outboxSentStatusUpdate);
            return Task.CompletedTask;
        }
        
        throw new InvalidOperationException($"Outbox item with ID {outboxSentStatusUpdate.OutboxItemId} not found.");
    }

    public Task<IEnumerable<OutboxBatchItem>> GetNextBatchAsync(int batchSize, IDbTransactionWrapped dbTransactionWrapped)
    {
        var outboxBatchItems = OutboxItems.Values
            .Where(x => x.StatusUpdates.Count == 0 
                || x.StatusUpdates.Last().Status == OutboxSentStatus.TransientFailure
                || (x.StatusUpdates.Last().Status == OutboxSentStatus.Scheduled && x.StatusUpdates.Last().NotBefore >= DateTimeOffset.UtcNow))
            .Take(batchSize)
            .ToList();

        var outboxBatchItemList = outboxBatchItems.Select(x => 
                new OutboxBatchItem(
                    x.OutboxItem.Id, 
                    x.OutboxItem.AssociatedId, 
                    x.OutboxItem.TypeName, 
                    x.OutboxItem.SerialisedData, 
                    x.OutboxItem.MessagingEntityName, 
                    "{}", 
                    x.OutboxItem.Created, 
                    x.StatusUpdates.LastOrDefault()?.Status ?? OutboxSentStatus.Pending, 
                    x.StatusUpdates.LastOrDefault()?.NotBefore));

        return Task.FromResult(outboxBatchItemList);
    }
}

public class OutboxItemWithSentStatuses
{
    public OutboxItem OutboxItem { get; init; }
    public List<OutboxSentStatusUpdate> StatusUpdates { get; } = [];

    public OutboxItemWithSentStatuses(OutboxItem outboxItem)
    {
        OutboxItem = outboxItem;
    }
}