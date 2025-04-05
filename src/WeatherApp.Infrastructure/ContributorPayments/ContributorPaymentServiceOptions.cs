namespace WeatherApp.Infrastructure.ContributorPayments;

public class ContributorPaymentServiceOptions
{
    public static string ConfigSectionName => "ContributorPaymentServiceOptions";

    public string? BaseUrl { get; set; }
    public string? SubscriptionKey { get; set; }
    public int MaxRetryCount { get; set; } = 3;
    public string ApiManagerSubscriptionKeyHeader { get; set; } = "API-Key";
}