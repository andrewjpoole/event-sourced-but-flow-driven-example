namespace WeatherApp.Infrastructure.Outbox;

public enum OutboxSentStatus
{
    Pending = 0,
    Sent = 1,
    TransientFailure = 2,
    Scheduled = 3,
    Cancelled = 4
}