using Dapper;
using WeatherApp.Domain.EventSourcing;
using WeatherApp.Infrastructure.RetryableDapperConnection;

namespace WeatherApp.Infrastructure.Persistence;

public class EventRepositorySql(IDbConnectionFactory dbConnectionFactory, IDbQueryProvider dbQueryProvider) : IEventRepository
{
    public async Task<PersistedEventResult> InsertEvent(Event @event)
    {
        using var connection = dbConnectionFactory.Create();
        return await InsertEventInternal(connection, @event.StreamId, @event.Version, @event.EventClassName, @event.SerialisedEvent);
    }

    public async Task<PersistedEventsResult> InsertEvents(IList<Event> events)
    {
        using var connection = dbConnectionFactory.Create();
        var newPersistedEvents = new List<PersistedEvent>();
        foreach (var @event in events)
        {
            var result = await InsertEventInternal(connection, @event.StreamId, @event.Version, @event.EventClassName, @event.SerialisedEvent);
            if (result.TryGetPersistedEvent(out var persistedEvent))
            {
                newPersistedEvents.Add(persistedEvent);
            }
            else
            {
                return PersistedEventsResult.FromError($"Unable to insert events. {result.Error}");
            }

        }

        return PersistedEventsResult.FromSuccess(newPersistedEvents);
    }


    private async Task<PersistedEventResult> InsertEventInternal(
        IRetryableConnection connection, 
        Guid streamId, 
        int version, 
        string eventClassName, 
        string serialisedEvent, 
        IDbTransactionWrapped? transaction = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventClassName);
        ArgumentException.ThrowIfNullOrWhiteSpace(serialisedEvent);
        ArgumentOutOfRangeException.ThrowIfLessThan(version, 1);

        try
        {
            var dynamicParameters = new DynamicParameters();
            dynamicParameters.Add(QueryParameters.StreamId, streamId);
            dynamicParameters.Add(QueryParameters.Version, version);
            dynamicParameters.Add(QueryParameters.EventClassName, eventClassName);
            dynamicParameters.Add(QueryParameters.SerialisedEvent, serialisedEvent);

            var dataRow = await connection.QuerySingleOrDefault(
                dbQueryProvider.InsertDomainEvent,
                dynamicParameters,
                transaction);
            var persistedEvent = (PersistedEvent)PersistedEventMapper.MapFromDynamic(dataRow);

            return PersistedEventResult.FromSuccess(persistedEvent);
        }
        catch (DbException e) when (e.ErrorNumber == 2601)
        {
            return PersistedEventResult.FromError("Event Version Conflict");
        }
    }

    public async Task<IEnumerable<PersistedEvent>> FetchEvents(Guid streamId)
    {
        using var connection = dbConnectionFactory.Create();

        var dynamicParameters = new DynamicParameters();
        dynamicParameters.Add(QueryParameters.StreamId, streamId);

        var rows = await connection.QueryAsync(dbQueryProvider.FetchDomainEventsByStreamId, dynamicParameters);
        return (IEnumerable<PersistedEvent>)rows.Select(r => PersistedEventMapper.MapFromDynamic(r));
    }
}