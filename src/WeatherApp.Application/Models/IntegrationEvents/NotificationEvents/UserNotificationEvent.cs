using System;

namespace WeatherApp.Application.Models.IntegrationEvents.NotificationEvents;

public record UserNotificationEvent(string Body, DateTimeOffset Timestamp);
