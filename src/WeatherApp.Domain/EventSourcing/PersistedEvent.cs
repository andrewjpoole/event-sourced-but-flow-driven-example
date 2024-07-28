using System.Text.Json;

namespace WeatherApp.Domain.EventSourcing;

public class PersistedEvent(long id, 
                            Guid streamId, 
                            int version, 
                            string eventClassName, 
                            string serialisedEvent, 
                            DateTime timestampCreatedUtc, 
                            Dictionary<string, object>? additionalFields = null)
    : Event(streamId, 
            version, 
            eventClassName, 
            serialisedEvent,
            additionalFields)
{
    public long Id { get; } = id;
    public DateTime TimestampCreatedUtc { get; } = timestampCreatedUtc;

    public T To<T>()
    {
        if (Value != null) return (T)Value;

        var value = JsonSerializer.Deserialize<T>(SerialisedEvent, GlobalJsonSerialiserSettings.Default) ?? 
                    throw new Exception($"SerialisedEvent could not be de-serialised into {typeof(T).FullName}.");

        Value = value;

        return (T)Value;
    }

    public override string ToString() => EventClassName;
}