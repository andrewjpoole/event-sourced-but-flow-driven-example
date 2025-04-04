﻿using WeatherApp.Domain;
using WeatherApp.Domain.Outcomes;
#pragma warning disable CS1998

namespace WeatherApp.Application.Services;

public class WeatherForecastGenerator : IWeatherForecastGenerator
{
    private readonly string[] summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    public async Task<OneOf<WeatherReportDetails, Failure>> Generate(WeatherReportDetails report)
    {
        report.Set(summaries[Random.Shared.Next(summaries.Length)], Random.Shared.Next(-20, 55));
        return report;
    }
}

public interface IWeatherForecastGenerator
{
    Task<OneOf<WeatherReportDetails, Failure>> Generate(WeatherReportDetails report);
}