namespace WeatherApp.Infrastructure.ApiClients.NotificationService;

public class NotificationsServiceOptions
{
    public static string ConfigSectionName => "NotificationsServiceOptions";

    public string? BaseUrl { get; set; }
    public string? SubscriptionKey { get; set; }
    public int MaxRetryCount { get; set; } = 3;
    public string ApiManagerSubscriptionKeyHeader { get; set; } = "API-Key";
}