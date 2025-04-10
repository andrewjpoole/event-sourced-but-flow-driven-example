namespace WeatherApp.Application.Models.IntegrationEvents.NotificationEvents;

public record UserNotificationEvent(string Body, string Reference, DateTimeOffset Timestamp);
