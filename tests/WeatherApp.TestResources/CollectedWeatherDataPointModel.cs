
public record CollectedWeatherDataPointModel(
    DateTimeOffset time,
    decimal WindSpeedInMetersPerSecond,
    string WindDirection,
    decimal TemperatureReadingInDegreesCelcius,
    decimal HumidityReadingInPercent
    );

public record CollectedWeatherDataModel(List<CollectedWeatherDataPointModel> Points);

public record WeatherReportResponse(string RequestedRegion, DateTime RequestedDate, Guid RequestId, int Temperature, string Summary);
