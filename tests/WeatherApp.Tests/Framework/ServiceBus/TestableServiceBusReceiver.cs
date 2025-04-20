// using Azure.Messaging.ServiceBus;

// namespace WeatherApp.Tests.Framework.ServiceBus;

// public class TestableServiceBusReceiver : ServiceBusReceiver
// {
//     private readonly string queueName;
    
//     public string QueueName => queueName;
//     public Queue<TestableProcessMessageEventArgs> Messages = [];

//     public TestableServiceBusReceiver(string queueName) : base()
//     {
//         this.queueName = queueName;
//     }

//     public override Task<ServiceBusReceivedMessage> ReceiveMessageAsync(
//         TimeSpan? maxWaitTime = default,
//         CancellationToken cancellationToken = default)
//     {
//         if(Messages.Count == 0)
//             return Task.FromResult<ServiceBusReceivedMessage>(null!);

//         var message = Messages.Dequeue();
//         var serviceBusMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
//             body: message.Message.Body,
//             correlationId: message.Message.CorrelationId,
//             properties: message.Message.ApplicationProperties,
//             deliveryCount: message.Message.DeliveryCount);

        
//     }

//     public override Task<ServiceBusReceivedMessage> PeekMessageAsync(long? fromSequenceNumber = null, CancellationToken cancellationToken = default)
//     {
//         return base.PeekMessageAsync(fromSequenceNumber, cancellationToken);
//     }

    

//     public override Task CompleteMessageAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default)
//     {
//         return base.CompleteMessageAsync(message, cancellationToken);
//     }

//     public override Task DeferMessageAsync(ServiceBusReceivedMessage message, IDictionary<string, object> propertiesToModify = null, CancellationToken cancellationToken = default)
//     {
//         return base.DeferMessageAsync(message, propertiesToModify, cancellationToken);
//     }

//     public override Task AbandonMessageAsync(ServiceBusReceivedMessage message, IDictionary<string, object> propertiesToModify = null, CancellationToken cancellationToken = default)
//     {
//         return base.AbandonMessageAsync(message, propertiesToModify, cancellationToken);
//     }

//     public override Task DeadLetterMessageAsync(ServiceBusReceivedMessage message, string deadLetterReason = null, string deadLetterDescription = null, CancellationToken cancellationToken = default)
//     {
//         return base.DeadLetterMessageAsync(message, deadLetterReason, deadLetterDescription, cancellationToken);
//     }

    
// }