namespace WeatherApp.Domain.Outcomes;

public class WeatherModelingServiceRejectionFailure(string message)
{
    public string Message { get; } = message;
}