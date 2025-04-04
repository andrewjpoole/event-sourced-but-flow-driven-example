namespace WeatherApp.Domain.Outcomes;

public class ContributorPaymentServiceFailure(string message)
{
    public string Message { get; } = message;
}