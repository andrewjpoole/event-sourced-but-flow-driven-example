namespace WeatherApp.Domain.Exceptions;

public class DomainValidationException : Exception
{
    public DomainValidationException(string message) : base(message)
    {
    }
}
public class ExpectedEventsNotFoundException : Exception
{
}