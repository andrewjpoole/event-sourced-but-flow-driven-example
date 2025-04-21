using Microsoft.Extensions.Logging;
using WeatherApp.Domain.ValueObjects;

namespace WeatherApp.Domain.Logging;

public static partial class LoggerMessages
{
    [LoggerMessage(Message = "Starting {Stage}")]
    public static partial void LogStartupMessage(this ILogger logger, string stage = "", LogLevel level = LogLevel.Information);


    [LoggerMessage(LogLevel.Critical, Message = "Failed to connect to SQL Server Database")]
    public static partial void LogFailedToConectToDatabase(this ILogger logger, Exception ex);
}

public static partial class OutboxLoggerMessages
{
    [LoggerMessage(Message = "Found {OutboxItemCount} Outbox Item(s) to dispatch")]
    public static partial void LogOutboxItemCount(this ILogger logger, int outboxItemCount, LogLevel level = LogLevel.Trace);


    [LoggerMessage(Level =LogLevel.Warning, Message = "Failed to send message for OutboxItemId: {OutboxItemId}")]
    public static partial void LogFailedToSendOutboxItem(this ILogger logger, Exception ex, long outboxItemId);


    [LoggerMessage(LogLevel.Information, Message = "Dispatched message for OutboxItemId: {OutboxItemId}")]
    public static partial void LogDispatchedOutboxItem(this ILogger logger, long outboxItemId);
}

public static partial class OrchestrationLoggerMessages
{
    
    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed idempotency checks, found existing aggregate(s) with stream Id(s): {existingStreamIds} for requestId: {RequestId} and Reference: {Reference}")]
    public static partial void LogFailedIdempotencyChecks(this ILogger logger, Guid requestId, string reference, string existingStreamIds);
    
    
    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to persist domain event and create outbox item {StreamId}")]
    public static partial void LogFailedToPersistDomainEventAndOutboxItem(this ILogger logger, Exception ex, Guid streamId);
    

    [LoggerMessage(Level =LogLevel.Information, Message = "Persisted {EventTypeName} domain event and outbox item for StreamId:{StreamId}, OutboxId:{OutboxId}")]
    public static partial void LogPersistedDomainEventAndOutboxItem(this ILogger logger, string eventTypeName, Guid streamId, long outboxId);


    [LoggerMessage(Level =LogLevel.Error, Message = "Failed contributor payment {RequestDescription} request for {StreamId} {pendingPayment}")]
    public static partial void LogFailedContributerPaymentRequest(this ILogger logger, string requestDescription, Guid streamId, PendingContributorPayment pendingPayment);


    [LoggerMessage(Level =LogLevel.Information, Message = "Orchestration update for {StreamId} {Update}")]
    public static partial void LogOrchestrationUPdate(this ILogger logger, Guid streamId, string update);


     [LoggerMessage(Level = LogLevel.Information, Message = "Received weather data for reference {Reference} @ location: {Location} with {RequestId}")]
    public static partial void LogReceivedWeatherData(this ILogger logger, string reference, string location, Guid requestId);


    [LoggerMessage(Level = LogLevel.Information, Message = "Weather data validation passed for location: {Location} RequestId: {RequestId}")]
    public static partial void LogWeatherDataValidationPassed(this ILogger logger, string location, Guid requestId);


    [LoggerMessage(Level = LogLevel.Information, Message = "Received ModelingDataRejectedIntegrationEvent for streamId: {StreamId}")]
    public static partial void LogReceivedModelingDataRejectedEvent(this ILogger logger, Guid streamId);


    [LoggerMessage(Level = LogLevel.Information, Message = "Received ModelingDataAcceptedIntegrationEvent for streamId: {StreamId}")]
    public static partial void LogReceivedModelingDataAcceptedEvent(this ILogger logger, Guid streamId);


    [LoggerMessage(Level = LogLevel.Information, Message = "Received ModelUpdatedIntegrationEvent for streamId: {StreamId}")]
    public static partial void LogReceivedModelUpdatedEvent(this ILogger logger, Guid streamId);

}

public static partial class WeatherDataCollectionLoggerMessages
{
    [LoggerMessage(Level =LogLevel.Information, Message = "Weather data submitted to modeling service for location:{Location} StreamId: {StreamId}")]
    public static partial void LogWeatherDataSubmittedToModelingService(this ILogger logger, string location, Guid streamId);    
}

public static partial class ServiceBusLoggerMessages
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Wiring up ServiceBusEventListener for {QueueOrTopicName}.")]
    public static partial void LogServiceBusListenerStartup(this ILogger logger, string queueOrTopicName);


    [LoggerMessage(Level = LogLevel.Information, Message = "Starting to consume from Queue: {QueueOrTopicName}")]
    public static partial void LogStartingToConsumeQueue(this ILogger logger, string queueOrTopicName);


    [LoggerMessage(Level = LogLevel.Information, Message = "Entering keepalive for {QueueOrTopicName}")]
    public static partial void LogEnteringKeepalive(this ILogger logger, string queueOrTopicName);


    [LoggerMessage(Level = LogLevel.Error, Message = "Exception thrown during startup.")]
    public static partial void LogStartupException(this ILogger logger, Exception exception);


    [LoggerMessage(Level = LogLevel.Error, Message = "JsonReaderException: Invalid JSON payload for message of type {MessageType}. Message will be dead-lettered.")]
    public static partial void LogJsonReaderException(this ILogger logger, string messageType, Exception exception);


    [LoggerMessage(Level = LogLevel.Error, Message = "PermanentException: Unable to process message. Message will be dead-lettered.")]
    public static partial void LogPermanentException(this ILogger logger, Exception exception);


    [LoggerMessage(Level = LogLevel.Error, Message = "Error processing message. Message will be retried.")]
    public static partial void LogProcessingError(this ILogger logger, Exception exception);


    [LoggerMessage(Level = LogLevel.Error, Message = "Error handling message: {ExceptionMessage}")]
    public static partial void LogErrorHandler(this ILogger logger, string exceptionMessage);
}
