namespace WeatherApp.Infrastructure.Persistence;

public class DbQueryProvider : IDbQueryProvider
{
    public const string InsertDomainEventQuery = @$"
        INSERT INTO DomainEvents({nameof(QueryParameters.StreamId)}, {nameof(QueryParameters.Version)}, {nameof(QueryParameters.EventClassName)}, {nameof(QueryParameters.SerialisedEvent)})
        VALUES({QueryParameters.StreamId}, {QueryParameters.Version}, {QueryParameters.EventClassName}, {QueryParameters.SerialisedEvent});
        SELECT Id, {nameof(QueryParameters.StreamId)}, {nameof(QueryParameters.Version)}, {nameof(QueryParameters.EventClassName)}, {nameof(QueryParameters.SerialisedEvent)}, TimestampCreatedUtc
        FROM DomainEvents
        WHERE Id = SCOPE_IDENTITY();";

    public const string FetchDomainEventByStreamIdQuery = @$"
        SELECT Id, {nameof(QueryParameters.StreamId)}, {nameof(QueryParameters.Version)}, {nameof(QueryParameters.EventClassName)}, {nameof(QueryParameters.SerialisedEvent)}, TimestampCreatedUtc
        FROM DomainEvents
        WHERE StreamId = {QueryParameters.StreamId}
        ORDER BY Id ASC;";

    public string InsertDomainEvent => InsertDomainEventQuery;
    public string FetchDomainEventsByStreamId => FetchDomainEventByStreamIdQuery;
}