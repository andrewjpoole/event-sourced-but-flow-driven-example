namespace WeatherApp.Infrastructure.Persistence;

public interface IDbQueryProvider
{
    string InsertDomainEvent { get; }
    string FetchDomainEventsByStreamId { get; }
    string FetchDomainEventByIdempotencyKey { get; }
}