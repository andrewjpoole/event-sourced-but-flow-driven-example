using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WeatherApp.Infrastructure.Messaging;
using WeatherApp.Infrastructure.Persistence;
using WeatherApp.Domain.Logging;
using System.Diagnostics;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry;
using System.Text.Json;

namespace WeatherApp.Infrastructure.Outbox;

public class OutboxDispatcherHostedService(
    ILogger<OutboxDispatcherHostedService> logger, 
    IDbConnectionFactory dbConnectionFactory,
    IOutboxBatchRepository outboxBatchRepository, 
    IOutboxRepository outboxRepository, 
    IUniversalMessageSender messageSender, 
    TimeProvider timeProvider, 
    IOptions<OutboxProcessorOptions> options) : IHostedService
{
    private readonly ILogger<OutboxDispatcherHostedService> logger = logger;
    private readonly IDbConnectionFactory dbConnectionFactory = dbConnectionFactory;
    private readonly IOutboxBatchRepository outboxBatchRepository = outboxBatchRepository;
    private readonly IOutboxRepository outboxRepository = outboxRepository;
    private readonly IUniversalMessageSender messageSender = messageSender;
    private readonly TimeProvider timeProvider = timeProvider;
    private readonly OutboxProcessorOptions options = options.Value;

    private CancellationTokenSource? cancellationTokenSource;
    private Task? backgroundTask;

    private static readonly ActivitySource Activity = new(nameof(OutboxDispatcherHostedService));
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting OutboxDispatcherHostedService...");

        cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Start the background task
        backgroundTask = Task.Run(() => RunBackgroundTaskAsync(cancellationTokenSource.Token), cancellationTokenSource.Token);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Logic to stop the outbox dispatcher service
        return Task.CompletedTask;
    }

    private async Task RunBackgroundTaskAsync(CancellationToken cancellationToken)
    {
        // Apply initial jitter so multiple dispatcher instances don't all start at the same time
        try
        {
            var jitterSeconds = options.InitialJitterSeconds;
            if (jitterSeconds > 0)
            {
                var jitter = Random.Shared.Next(0, jitterSeconds + 1);
                logger.LogDebug("Applying initial dispatcher jitter of {jitter}s", jitter);
                await Task.Delay(TimeSpan.FromSeconds(jitter), timeProvider, cancellationToken);
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
                await Task.Delay(TimeSpan.FromSeconds(options.IntervalBetweenBatchesInSeconds), 
                    timeProvider, cancellationToken);
                    
                await ProcessOutboxBatchAsync(options.BatchSize, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing outbox batch");
                throw;
            }
        }
    }


    private async Task ProcessOutboxBatchAsync(int batchSize, CancellationToken cancellationToken)
    {
        using var connection = dbConnectionFactory.Create();
        var transaction = connection.BeginTransaction();

        var outboxItems = await outboxBatchRepository.GetNextBatchAsync(batchSize, transaction);
        logger.LogOutboxItemCount(outboxItems.Count());

        foreach (var item in outboxItems)
        {
            try
            {
                //*
                // Hydrate the telemetry trace context.
                var parentContext = Propagator.Extract(default, item.SerialisedTelemetry, (serialisedTelemetry, key) => 
                {
                    var telemetryDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(serialisedTelemetry);

                    if (telemetryDictionary == null || telemetryDictionary.Count == 0)
                        return Enumerable.Empty<string>();

                    if(telemetryDictionary.TryGetValue(key, out var value))
                        return new List<string> { telemetryDictionary[key] };

                    return Enumerable.Empty<string>();
                });

                // Hydrate trace context to cross the Outbox 'airgap'
                Baggage.Current = parentContext.Baggage;

                using var activity = Activity.StartActivity("Dispatch Message", ActivityKind.Consumer, parentContext.ActivityContext);
                //-
                
                await messageSender.SendAsync(item.SerialisedData, item.MessagingEntityName, cancellationToken);
                await outboxRepository.AddSentStatus(OutboxSentStatusUpdate.CreateSent(item.Id));
                logger.LogDispatchedOutboxItem(item.Id);
            }
            catch (Exception ex)
            {
                logger.LogFailedToSendOutboxItem(ex, item.Id);
                await outboxRepository.AddSentStatus(OutboxSentStatusUpdate.CreateTransientFailure(item.Id, timeProvider.GetUtcNow().AddMinutes(5)));
            }            
        }

        transaction.Commit();
    }    
}
