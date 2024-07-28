namespace WeatherApp.Domain.Exceptions;

public class DomainValidationException(string message) : Exception(message);
public class ExpectedEventsNotFoundException : Exception
{
}