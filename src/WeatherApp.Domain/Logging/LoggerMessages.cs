using Microsoft.Extensions.Logging;
using WeatherApp.Domain.ValueObjects;

namespace WeatherApp.Domain.Logging;

public static partial class LoggerMessages
{
    [LoggerMessage(Message = "starting {Stage}")]
    public static partial void LogStartupMessage(this ILogger logger, string stage = "", LogLevel level = LogLevel.Information);

    [LoggerMessage(LogLevel.Critical, Message = "Failed to connect to SQL Server Database")]
    public static partial void LogFailedToConectToDatabase(this ILogger logger, Exception ex);
}

public static partial class OutboxLoggerMessages
{
    [LoggerMessage(Message = "Found {OutboxItemCount} Outbox Item(s) to dispatch")]
    public static partial void LogOutboxItemCount(this ILogger logger, int outboxItemCount, LogLevel level = LogLevel.Information);

    [LoggerMessage(Level =LogLevel.Warning, Message = "Failed to send message for OutboxItemId: {OutboxItemId}")]
    public static partial void LogFailedToSendOutboxItem(this ILogger logger, Exception ex, long outboxItemId);

    [LoggerMessage(LogLevel.Information, Message = "Dispatched message for OutboxItemId: {OutboxItemId}")]
    public static partial void LogDispatchedOutboxItem(this ILogger logger, long outboxItemId);
}

public static partial class OrchestrationLoggerMessages
{
    [LoggerMessage(Level =LogLevel.Error, Message = "Failed to persist domain event and create outbox item {StreamId}")]
    public static partial void LogFailedToPersistDomainEventAndOutboxItem(this ILogger logger, Exception ex, Guid streamId);

    [LoggerMessage(Level =LogLevel.Error, Message = "Failed contributor payment {RequestDescription} request for {StreamId} {pendingPayment}")]
    public static partial void LogFailedContributerPaymentRequest(this ILogger logger, string requestDescription, Guid streamId, PendingContributorPayment pendingPayment);
    
}
