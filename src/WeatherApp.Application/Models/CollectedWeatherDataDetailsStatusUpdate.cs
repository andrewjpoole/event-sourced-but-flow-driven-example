namespace WeatherApp.Application.Models;

public record CollectedWeatherDataDetailsStatusUpdate(Guid CollectedWeatherDetailsRequestId, DateTime TimeStamp, string EventName, string ExtraInfo = "");