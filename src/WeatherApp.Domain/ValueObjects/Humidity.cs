﻿using WeatherApp.Domain.BusinessRules;

namespace WeatherApp.Domain.ValueObjects;

public record Humidity
{
    public decimal Value { get; }

    public string Unit => "%";

    public Humidity(decimal value)
    {
        var humidity = value;
        Rules.DecimalRules.CheckPositive(humidity);
        Rules.DecimalRules.CheckIsWithinRange(humidity, Unit, 100M);

        Value = value;
    }
}
