using System.Collections.Immutable;
using WeatherApp.Domain.EventSourcing;

namespace WeatherApp.Infrastructure.Persistence;

public static class PersistedEventMapper
{
    private static readonly ImmutableList<string> StandardFieldNames = ImmutableList.Create(
        "Id", "ID", "id",
        nameof(QueryParameters.StreamId),
        nameof(QueryParameters.Version),
        nameof(QueryParameters.EventClassName),
        nameof(QueryParameters.SerialisedEvent),
        nameof(QueryParameters.TimestampCreatedUtc)
        );

    public static PersistedEvent MapFromDynamic(dynamic dataRow)
    {
        var id = dataRow.Id ?? dataRow.ID ?? dataRow.id ?? throw new Exception("Failed to parse ID");
        var streamId = dataRow.StreamId;
        var version = dataRow.Version;
        var eventClassName = dataRow.EventClassName;
        var serialisedEvent = dataRow.SerialisedEvent;
        var timestampCreatedUtc = dataRow.TimestampCreatedUtc;
       
        // Any additional fields go into the additional fields dictionary...
        var dataRowDictionary = (IDictionary<string, object>)dataRow;
        var additionalFields = dataRowDictionary.Where(x => StandardFieldNames.Contains(x.Key) == false).ToDictionary(k => k.Key, v => v.Value);

        var persistedEvent = new PersistedEvent(id, streamId, version, eventClassName, serialisedEvent, timestampCreatedUtc, additionalFields);

        return persistedEvent;
    }
}