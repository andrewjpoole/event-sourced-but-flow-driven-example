﻿using WeatherApp.Domain.BusinessRules;

namespace WeatherApp.Domain.ValueObjects;

public record Temperature
{
    public decimal Value { get; }

    public string Unit => "°C";

    public Temperature(decimal value)
    {
        var temperature = value;
        Rules.DecimalRules.CheckIsWithinRange(temperature, Unit, 50.0M, -30.0M);

        Value = value;
    }
}