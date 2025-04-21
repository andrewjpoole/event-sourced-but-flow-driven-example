namespace WeatherApp.Domain.Outcomes;

public class AlreadyProcessedFailure(string message)
{
    public string Message { get; } = message;
}