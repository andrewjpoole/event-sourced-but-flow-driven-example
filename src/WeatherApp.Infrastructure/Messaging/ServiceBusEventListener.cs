using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WeatherApp.Application.Exceptions;
using WeatherApp.Application.Services;
using WeatherApp.Domain.Logging;

namespace WeatherApp.Infrastructure.Messaging;

public class ServiceBusEventListener<T> : IHostedService, IDisposable
    where T : class
{
    private readonly ILogger<ServiceBusEventListener<T>> logger;
    private readonly IEventHandler<T> eventHandler;
    private readonly ServiceBusClient serviceBusClient;
    private ServiceBusProcessor? serviceBusProcessor;

    private readonly string queueOrTopicName;
    private readonly int maxConcurrentCalls;
    private readonly int initialBackoffInMs;
    private readonly int maxJitterInMs;

    public ServiceBusEventListener(
        ServiceBusClient serviceBusClient,
        IOptions<ServiceBusInboundOptions> options,
        IEventHandler<T> eventHandler,
        ILogger<ServiceBusEventListener<T>> logger)
    {
        this.logger = logger;
        this.serviceBusClient = serviceBusClient;
        this.eventHandler = eventHandler;
        initialBackoffInMs = options.Value.InitialBackoffInMs;
        maxConcurrentCalls = options.Value.MaxConcurrentCalls;
        maxJitterInMs = Convert.ToInt32(initialBackoffInMs * 0.1);
        
        var type = typeof(T);
        var entityNameFotTypeFromConfig = options.Value.ResolveQueueOrTopicNameFromConfig(type.Name);
        queueOrTopicName = new QueueOrTopicName(entityNameFotTypeFromConfig).Name;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogServiceBusListenerStartup(queueOrTopicName);

            serviceBusProcessor = serviceBusClient.CreateProcessor(queueOrTopicName, new ServiceBusProcessorOptions
            {
                PrefetchCount = 1,
                AutoCompleteMessages = false,
                MaxConcurrentCalls = maxConcurrentCalls
            });

            serviceBusProcessor.ProcessMessageAsync += MessageHandler;
            serviceBusProcessor.ProcessErrorAsync += ErrorHandler;

            logger.LogStartingToConsumeQueue(queueOrTopicName);

            await serviceBusProcessor.StartProcessingAsync(cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogStartupException(e);
            throw;
        }

        logger.LogEnteringKeepalive(queueOrTopicName);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (serviceBusProcessor is null)
            return;

        await serviceBusProcessor.StopProcessingAsync(cancellationToken);
        await serviceBusProcessor.CloseAsync(cancellationToken);
    }

    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
        // Add telemetry tracing code here

        try
        {
            var @event = args.Message.GetJsonPayload<T>();

            if (@event is null)
                throw new PermanentException("Unable to deserialize payload of ServiceBusReceivedMessage");

            await eventHandler.HandleEvent(@event);

            await args.CompleteMessageAsync(args.Message);
        }
        catch (JsonException e)
        {
            logger.LogJsonReaderException(typeof(T).Name, e);
            await args.DeadLetterMessageAsync(args.Message, e.Message);
        }
        catch (PermanentException e)
        {
            logger.LogPermanentException(e);
            await args.DeadLetterMessageAsync(args.Message, e.Message);
        }
        catch (Exception e)
        {
            logger.LogProcessingError(e);

            // Abandon so the message can be retried after a sensible delay
            await Task.Delay(GetCappedExponentialBackoffDelay(args.Message.DeliveryCount));
            await args.AbandonMessageAsync(args.Message);
        }
    }

    private int GetCappedExponentialBackoffDelay(int retryAttempt)
    {
        if (retryAttempt > 5)
            retryAttempt = 5;

        var jitterer = new Random();
        var exponential = Math.Pow(2, retryAttempt);
        var basicDelay = TimeSpan.FromMilliseconds(initialBackoffInMs * exponential);
        var jitteredDelay = basicDelay.Add(TimeSpan.FromMilliseconds(jitterer.Next(0, maxJitterInMs)));
        return (int)jitteredDelay.TotalMilliseconds;
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        logger.LogErrorHandler(args.Exception.Message);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        serviceBusProcessor?.DisposeAsync().ConfigureAwait(false);
    }
}