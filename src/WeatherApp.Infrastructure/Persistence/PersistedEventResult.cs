using System.Diagnostics.CodeAnalysis;
using WeatherApp.Domain.EventSourcing;

namespace WeatherApp.Infrastructure.Persistence;

public record PersistedEventResult
{
    public PersistedEvent? PersistedEvent { get; }
    public string? Error { get; }

    private PersistedEventResult(PersistedEvent? persistedEvent, string? error)
    {
        PersistedEvent = persistedEvent;
        Error = error;
    }

    public static PersistedEventResult FromSuccess(PersistedEvent persistedEvent) => new(persistedEvent, null);
    public static PersistedEventResult FromError(string error) => new(null, error);

    public bool TryGetPersistedEvent([NotNullWhen(true)] out PersistedEvent? persistedEvent)
    {
        persistedEvent = PersistedEvent;
        return PersistedEvent != null;
    }
}