﻿using System.Diagnostics;
using Dapper;
using WeatherApp.Domain.EventSourcing;
using WeatherApp.Infrastructure.RetryableDapperConnection;

namespace WeatherApp.Infrastructure.Persistence;

public class EventRepositorySql(IDbConnectionFactory dbConnectionFactory, IDbQueryProvider dbQueryProvider) : IEventRepository
{
    private static readonly ActivitySource Activity = new(nameof(EventRepositorySql));
    public async Task<PersistedEventResult> InsertEvent(Event @event)
    {
        using var connection = dbConnectionFactory.Create();
        return await InsertEventInternal(connection, @event.StreamId, @event.Version, @event.EventClassName, @event.SerialisedEvent);
    }

    public async Task<PersistedEventResult> InsertEvent(Event @event, IDbTransactionWrapped transaction)
    {
        var connection = transaction.GetConnection();
        return await InsertEventInternal(connection, @event.StreamId, @event.Version, @event.EventClassName, @event.SerialisedEvent, transaction);
    }

    public async Task<PersistedEventsResult> InsertEvents(IList<Event> events)
    {
        // todo: use db transaction? all or nothing?
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

            using var activity = Activity.StartActivity("Domain Event Insertion", ActivityKind.Producer);
            activity?.SetTag("domain-event.streamId", streamId.ToString());
            activity?.SetTag("domain-event.version", version.ToString());
            activity?.SetTag("domain-event.eventclassName", eventClassName);

            dynamic? dataRow = await connection.QuerySingleOrDefault(
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

        if (rows == null || !rows.Any())        
            return []; 

        var persistedEvents = rows.Select(r => (PersistedEvent)PersistedEventMapper.MapFromDynamic(r));
        return persistedEvents;
    }
    
    public async Task<IEnumerable<PersistedEvent>> FindExistingEventsByIdempotencyKey(string idempotencyKey)
    {
        using var connection = dbConnectionFactory.Create();

        var dynamicParameters = new DynamicParameters();
        dynamicParameters.Add(QueryParameters.IdempotencyKey, idempotencyKey);

        var rows = await connection.QueryAsync(dbQueryProvider.FetchDomainEventByIdempotencyKey, dynamicParameters);

        if (rows == null || !rows.Any())        
            return []; 

        var persistedEvents = rows.Select(r => (PersistedEvent)PersistedEventMapper.MapFromDynamic(r));
        return persistedEvents;
    }

    public IDbTransactionWrapped BeginTransaction()
    {
        var connection = dbConnectionFactory.Create();
        return connection.BeginTransaction();
    }    
}