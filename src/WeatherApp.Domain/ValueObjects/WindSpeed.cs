using WeatherApp.Domain.BusinessRules;

namespace WeatherApp.Domain.ValueObjects;

public record WindSpeed
{
    public decimal Value { get; }

    public string Unit => "m/s";

    public WindSpeed(decimal value)
    {
        var windspeed = value;
        Rules.DecimalRules.CheckPositive(windspeed);
        Rules.DecimalRules.CheckIsWithinRange(windspeed, Unit, 70M);

        Value = value;
    }
}