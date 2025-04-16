
public record CollectedWeatherDataPointModel(
    DateTimeOffset time,
    decimal WindSpeedInMetersPerSecond,
    string WindDirection,
    decimal TemperatureReadingInDegreesCelcius,
    decimal HumidityReadingInPercent
    );
