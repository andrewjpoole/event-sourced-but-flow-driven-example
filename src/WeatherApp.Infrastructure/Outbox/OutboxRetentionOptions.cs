namespace WeatherApp.Infrastructure.Outbox;

public class OutboxRetentionOptions
{
    /// <summary>
    /// Age after which sent outbox items will be removed.
    /// Default is 30 days.
    /// </summary>
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// How often to run the retention cleanup.
    /// Default is 1 day.
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Maximum initial jitter (in seconds) to apply before the first run, to avoid multiple
    /// instances performing cleanup simultaneously. Default is 300 seconds (5 minutes).
    /// </summary>
    public int InitialJitterSeconds { get; set; } = 300;
}
