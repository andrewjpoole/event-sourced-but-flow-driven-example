using WeatherApp.Domain.BusinessRules;

namespace WeatherApp.Domain.ValueObjects;

public record WindDirection
{
    public string Value { get; }

    public WindDirection(string value)
    {
        var windDirection = value;
        Rules.StringRules.CheckNotNullEmptyOrWhitespace(windDirection);
        Rules.StringRules.CheckMaxLength(windDirection, 5);

        // in future, accept decimal degrees or "SSW" etc?

        Value = value;
    }
}