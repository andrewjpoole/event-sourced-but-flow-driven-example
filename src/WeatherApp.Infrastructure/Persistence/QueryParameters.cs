namespace WeatherApp.Infrastructure.Persistence;

public static class QueryParameters
{
    public const string StreamId = "@StreamId";
    public const string Version = "@Version";
    public const string EventClassName = "@EventClassName";
    public const string SerialisedEvent = "@SerialisedEvent";
    public const string TimestampCreatedUtc = "@TimestampCreatedUtc";
}