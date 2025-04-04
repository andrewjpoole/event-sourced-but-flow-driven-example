using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WeatherApp.Infrastructure.MessageBus;
using WeatherApp.Infrastructure.Persistence;
using WeatherApp.Domain.Logging;

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
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Logic to start the outbox dispatcher service
        while(cancellationToken.IsCancellationRequested == false)
        {
            try
            {
                Task.Delay(TimeSpan.FromSeconds(options.IntervalBetweenBatchesInSeconds), cancellationToken).Wait(cancellationToken);
                ProcessOutboxBatchAsync(options.BatchSize, cancellationToken).Wait(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing outbox batch");
            }
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Logic to stop the outbox dispatcher service
        return Task.CompletedTask;
    }

    public async Task ProcessOutboxBatchAsync(int batchSize, CancellationToken cancellationToken)
    {
        using var connection = dbConnectionFactory.Create();
        var transaction = connection.BeginTransaction();

        var outboxItems = await outboxBatchRepository.GetNextBatchAsync(batchSize, transaction);
        logger.LogOutboxItemCount(outboxItems.Count());

        foreach (var item in outboxItems)
        {
            try
            {
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

        transaction.Rollback();
    }
}
