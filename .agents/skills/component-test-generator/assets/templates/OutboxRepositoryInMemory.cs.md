# OutboxRepositoryInMemory template

Purpose: provide a full in-memory implementation of outbox persistence so timer-driven workers can query, update, and purge outbox records without touching a real database. Only use this template when the target app actually has an outbox pattern.

```csharp
using {Namespace}.Infrastructure.Outbox;
using {Namespace}.Infrastructure.RetryableDapperConnection;

namespace {Namespace}.Tests.TUnit.Framework.Persistence;

public sealed class OutboxRepositoryInMemory : IOutboxRepository, IOutboxBatchRepository
{
    private readonly object _lock = new();

    public Dictionary<long, OutboxItemWithSentStatuses> OutboxItems { get; } = new();

    private long GetNextId() => OutboxItems.Count == 0
        ? Random.Shared.Next(1, 20_000)
        : OutboxItems.Keys.Max() + 1;

    public Task<long> Add(OutboxItem outboxItem, IDbTransactionWrapped? transaction = null)
    {
        lock (_lock)
        {
            var id = GetNextId();
            var newItem = new OutboxItemWithSentStatuses(outboxItem)
            {
                OutboxItem = outboxItem with { Id = id }
            };

            OutboxItems.Add(id, newItem);
            return Task.FromResult(id);
        }
    }

    public Task AddScheduled(OutboxItem outboxItem, DateTimeOffset retryAfter)
    {
        lock (_lock)
        {
            var id = GetNextId();
            var newItem = new OutboxItemWithSentStatuses(outboxItem)
            {
                OutboxItem = outboxItem with { Id = id }
            };

            newItem.StatusUpdates.Add(OutboxSentStatusUpdate.CreateScheduled(id, retryAfter));
            OutboxItems.Add(id, newItem);
            return Task.CompletedTask;
        }
    }

    public Task AddSentStatus(OutboxSentStatusUpdate statusUpdate, IDbTransactionWrapped? transaction = null)
    {
        lock (_lock)
        {
            if (!OutboxItems.TryGetValue(statusUpdate.OutboxItemId, out var item))
            {
                throw new InvalidOperationException($"Outbox item with ID {statusUpdate.OutboxItemId} was not found.");
            }

            item.StatusUpdates.Add(statusUpdate);
            return Task.CompletedTask;
        }
    }

    public Task<IEnumerable<OutboxBatchItem>> GetNextBatchAsync(int batchSize, IDbTransactionWrapped transaction)
    {
        lock (_lock)
        {
            var batch = OutboxItems.Values
                .Where(x => x.StatusUpdates.Count == 0
                    || x.StatusUpdates.Last().Status == OutboxSentStatus.TransientFailure
                    || (x.StatusUpdates.Last().Status == OutboxSentStatus.Scheduled
                        && x.StatusUpdates.Last().NotBefore >= DateTimeOffset.UtcNow))
                .Take(batchSize)
                .Select(x => new OutboxBatchItem(
                    x.OutboxItem.Id,
                    x.OutboxItem.AssociatedId,
                    x.OutboxItem.TypeName,
                    x.OutboxItem.SerialisedData,
                    x.OutboxItem.MessagingEntityName,
                    "{}",
                    x.OutboxItem.Created,
                    x.StatusUpdates.LastOrDefault()?.Status ?? OutboxSentStatus.Pending,
                    x.StatusUpdates.LastOrDefault()?.NotBefore))
                .ToList();

            return Task.FromResult(batch.AsEnumerable());
        }
    }

    public Task<int> RemoveSentOutboxItemsOlderThan(DateTimeOffset cutoff)
    {
        lock (_lock)
        {
            var idsToRemove = OutboxItems
                .Where(x => x.Value.StatusUpdates.Count > 0)
                .Where(x =>
                {
                    var latest = x.Value.StatusUpdates.LastOrDefault();
                    return latest is not null
                        && latest.Status == OutboxSentStatus.Sent
                        && latest.Created < cutoff;
                })
                .Select(x => x.Key)
                .ToList();

            foreach (var id in idsToRemove)
            {
                OutboxItems.Remove(id);
            }

            return Task.FromResult(idsToRemove.Count);
        }
    }
}

public sealed class OutboxItemWithSentStatuses
{
    public OutboxItemWithSentStatuses(OutboxItem outboxItem)
    {
        OutboxItem = outboxItem;
    }

    public OutboxItem OutboxItem { get; init; }
    public List<OutboxSentStatusUpdate> StatusUpdates { get; } = [];
}
```

Adaptation note:

Keep the implementation aligned with the real `IOutboxRepository` and `IOutboxBatchRepository` methods in the target solution. If the production interfaces expose more operations, add them here too.
