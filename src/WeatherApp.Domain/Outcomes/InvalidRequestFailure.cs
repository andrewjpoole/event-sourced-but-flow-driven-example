namespace WeatherApp.Domain.Outcomes;

public class InvalidRequestFailure(IDictionary<string, string[]> validationErrors)
{
    public IDictionary<string, string[]> ValidationErrors { get; } = validationErrors;

    public InvalidRequestFailure(string message) : this(new Dictionary<string, string[]>{{"model", new[] {message}}})
    {
    }
}
