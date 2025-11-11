using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WeatherApp.Infrastructure.Outbox;

public class OutboxRetentionHostedService(
    ILogger<OutboxRetentionHostedService> logger,
    IOutboxRepository outboxRepository,
    IOptions<OutboxRetentionOptions> options,
    TimeProvider timeProvider) : IHostedService, IDisposable
{
    private readonly ILogger<OutboxRetentionHostedService> logger = logger;
    private readonly IOutboxRepository outboxRepository = outboxRepository;
    private readonly OutboxRetentionOptions options = options.Value;
    private readonly TimeProvider timeProvider = timeProvider;

    private CancellationTokenSource? cts;
    private Task? backgroundTask;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting OutboxRetentionHostedService (cleanup every {interval})", options.CleanupInterval);
        cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        backgroundTask = Task.Run(() => RunAsync(cts.Token), cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (cts == null)
            return;

        cts.Cancel();
        try
        {
            if (backgroundTask != null)
                await Task.WhenAny(backgroundTask, Task.Delay(TimeSpan.FromSeconds(5), cancellationToken));
        }
        catch (OperationCanceledException) { }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        // Apply a small randomized initial delay so multiple instances don't run cleanup at the same instant
        try
        {
            var jitterSeconds = options.InitialJitterSeconds;
            if (jitterSeconds > 0)
            {
                var jitter = Random.Shared.Next(0, jitterSeconds + 1);
                logger.LogDebug("Applying initial jitter of {jitter}s before first retention run", jitter);
                await Task.Delay(TimeSpan.FromSeconds(jitter), cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var cutoff = timeProvider.GetUtcNow().Add(-options.RetentionPeriod);
                var removed = await outboxRepository.RemoveSentOutboxItemsOlderThan(cutoff);
                if (removed > 0)
                    logger.LogInformation("Removed {count} sent outbox items older than {cutoff}", removed, cutoff);

                await Task.Delay(options.CleanupInterval, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during outbox retention cleanup");
                // wait a bit before retrying to avoid tight loop on persistent failure
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }
    }

    public void Dispose()
    {
        cts?.Dispose();
    }
}
