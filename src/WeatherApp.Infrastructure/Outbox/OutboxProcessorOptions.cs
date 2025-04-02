namespace WeatherApp.Infrastructure.Outbox;

public class OutboxProcessorOptions
{
    public int BatchSize { get; set; } = 10;
    public int IntervalBetweenBatchesInSeconds { get; set; } = 5;    
}
