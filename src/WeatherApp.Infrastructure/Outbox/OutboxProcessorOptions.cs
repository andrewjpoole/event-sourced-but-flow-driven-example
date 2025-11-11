namespace WeatherApp.Infrastructure.Outbox;

public class OutboxProcessorOptions
{
    /// <summary>
    /// Number of outbox items to fetch and attempt to dispatch per batch. Default is 10.
    /// </summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>
    /// Delay in seconds between batch processing loops. Default is 5 seconds.
    /// </summary>
    public int IntervalBetweenBatchesInSeconds { get; set; } = 5;    

    /// <summary>
    /// Maximum initial jitter (in seconds) to wait before the dispatcher starts its loop. Default is 5 seconds.
    /// </summary>
    public int InitialJitterSeconds { get; set; } = 5;
}
