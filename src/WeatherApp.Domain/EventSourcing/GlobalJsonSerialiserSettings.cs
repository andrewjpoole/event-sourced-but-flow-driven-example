using System.Text.Json;

namespace WeatherApp.Domain.EventSourcing;

public static class GlobalJsonSerialiserSettings
{
    public static JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true, 
        IncludeFields = true
    };
}