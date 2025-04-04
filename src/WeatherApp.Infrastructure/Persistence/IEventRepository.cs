using System.Data;
using WeatherApp.Domain.EventSourcing;
using WeatherApp.Infrastructure.RetryableDapperConnection;

namespace WeatherApp.Infrastructure.Persistence;

public interface IEventRepository
{
    Task<PersistedEventResult> InsertEvent(Event @event);
    Task<PersistedEventResult> InsertEvent(Event @event, IDbTransactionWrapped transaction);
    Task<PersistedEventsResult> InsertEvents(IList<Event> events);
    Task<IEnumerable<PersistedEvent>> FetchEvents(Guid streamId);

    IDbTransactionWrapped BeginTransaction();
}