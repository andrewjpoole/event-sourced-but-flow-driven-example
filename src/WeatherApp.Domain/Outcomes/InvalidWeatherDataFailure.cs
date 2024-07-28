namespace WeatherApp.Domain.Outcomes;

public class InvalidWeatherDataFailure(string detail)
{
    public string Title => "Invalid WeatherData";

    public string Detail { get; } = detail;
}