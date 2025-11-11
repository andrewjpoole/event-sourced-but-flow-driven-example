using Azure.Messaging.ServiceBus;

namespace WeatherApp.Tests.TUnit.Framework.ServiceBus;

public class TestableMessageEventArgs(ServiceBusReceivedMessage message) 
    : ProcessMessageEventArgs(message, null, CancellationToken.None)
{
    public bool WasCompleted;
    public bool WasDeadLettered;
    public bool WasAbandoned;
    public DateTime Created = DateTime.UtcNow;
    public string DeadLetterReason = string.Empty;

    public override Task CompleteMessageAsync(ServiceBusReceivedMessage message,
        CancellationToken cancellationToken = new())
    {
        WasCompleted = true;
        return Task.CompletedTask;
    }

    public override Task DeadLetterMessageAsync(ServiceBusReceivedMessage message, string deadLetterReason,
        string? deadLetterErrorDescription = null, CancellationToken cancellationToken = new())
    {
        WasDeadLettered = true;
        DeadLetterReason = deadLetterReason;
        return Task.CompletedTask;
    }

    public override Task AbandonMessageAsync(ServiceBusReceivedMessage message, IDictionary<string, object>? propertiesToModify = null,
        CancellationToken cancellationToken = new())
    {
        WasAbandoned = true;
        return Task.CompletedTask;
    }
}