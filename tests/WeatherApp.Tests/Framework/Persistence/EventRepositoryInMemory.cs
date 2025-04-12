using Moq;
using WeatherApp.Domain.EventSourcing;
using WeatherApp.Infrastructure.Persistence;
using WeatherApp.Infrastructure.RetryableDapperConnection;

namespace WeatherApp.Tests.e2eComponentTests.Framework.Persistence;

public class EventRepositoryInMemory : IEventRepository
{
    public List<PersistedEvent> PersistedEvents { get; } = [];

    public List<FakeDbTransactionWrapped> Transactions { get; } = new();
    
    public Task<PersistedEventResult> InsertEvent(Event @event)
    {
        var newPersistedEvent = new PersistedEvent(PersistedEvents.Count, @event.StreamId, @event.Version, @event.EventClassName, @event.SerialisedEvent, DateTime.UtcNow);
        PersistedEvents.Add(newPersistedEvent);
        return Task.FromResult(PersistedEventResult.FromSuccess(newPersistedEvent));
    }

    public Task<PersistedEventsResult> InsertEvents(IList<Event> events)
    {
        var newPersistedEvents = new List<PersistedEvent>();
        foreach (var @event in events)
        {
            var newPersistedEvent = new PersistedEvent(PersistedEvents.Count, @event.StreamId, @event.Version, @event.EventClassName, @event.SerialisedEvent, DateTime.UtcNow);
            PersistedEvents.Add(newPersistedEvent);
            newPersistedEvents.Add(newPersistedEvent);
        }
        return Task.FromResult(PersistedEventsResult.FromSuccess(newPersistedEvents));
    }
    public Task<IEnumerable<PersistedEvent>> FetchEvents(Guid streamId)
    {
        return Task.FromResult(PersistedEvents.Where(pe => pe.StreamId == streamId));
    }

    public Task<PersistedEventResult> InsertEvent(Event @event, IDbTransactionWrapped transaction)
    {
        return InsertEvent(@event);
    }

    public IDbTransactionWrapped BeginTransaction()
    {
        var transaction = new FakeDbTransactionWrapped();
        Transactions.Add(transaction);
        return transaction;
    }
}

public class FakeDbTransactionWrapped : IDbTransactionWrapped
{
    public bool WasCommitted { get; private set; } = false;
    public bool WasRolledBack { get; private set; } = false;

    public void Commit()
    {
        WasCommitted = true;
    }

    public IRetryableConnection GetConnection()
    {
        return new Mock<IRetryableConnection>().Object;
    }

    public void Rollback()
    {
        WasRolledBack = true;
    }

    public System.Data.IDbTransaction ToIDbTransaction()
    {
        return new Mock<System.Data.IDbTransaction>().Object;
    }
}