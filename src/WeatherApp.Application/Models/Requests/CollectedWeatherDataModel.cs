﻿using WeatherApp.Domain.Entities;
using WeatherApp.Domain.ValueObjects;

namespace WeatherApp.Application.Models.Requests;

public record CollectedWeatherDataPointModel(
    DateTimeOffset time,
    decimal WindSpeedInMetersPerSecond,
    string WindDirection,
    decimal TemperatureReadingInDegreesCelcius,
    decimal HumidityReadingInPercent
    );

public record CollectedWeatherDataModel(List<CollectedWeatherDataPointModel> Points)
{
    public CollectedWeatherData ToEntity()
    {
        var points = Points.Select(collectedReading => 
            new CollectedWeatherDataPoint(
                Guid.NewGuid(), 
                collectedReading.time, 
                new WindSpeed(collectedReading.WindSpeedInMetersPerSecond), 
                new WindDirection(collectedReading.WindDirection), 
                new Temperature(collectedReading.TemperatureReadingInDegreesCelcius), 
                new Humidity(collectedReading.HumidityReadingInPercent))).ToList();
        var collectedDataEntity = new CollectedWeatherData(points);
        return collectedDataEntity;
    }
}