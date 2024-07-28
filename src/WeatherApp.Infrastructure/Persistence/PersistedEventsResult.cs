using System.Diagnostics.CodeAnalysis;
using WeatherApp.Domain.EventSourcing;

namespace WeatherApp.Infrastructure.Persistence;

public record PersistedEventsResult
{
    public List<PersistedEvent>? PersistedEvents { get; }
    public string? Error { get; }

    private PersistedEventsResult(List<PersistedEvent>? persistedEvents, string? error)
    {
        PersistedEvents = persistedEvents;
        Error = error;
    }

    public static PersistedEventsResult FromSuccess(List<PersistedEvent>? persistedEvents) => new(persistedEvents, null);
    public static PersistedEventsResult FromError(string error) => new(null, error);

    public bool TryGetPersistedEvents([NotNullWhen(true)] out List<PersistedEvent>? persistedEvents)
    {
        persistedEvents = PersistedEvents;
        return PersistedEvents != null;
    }
}