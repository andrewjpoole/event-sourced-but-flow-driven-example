using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace WeatherApp.Tests.Framework.ServiceBus;

public class FakeServiceBus(Func<string, string> GetTypeNameFromEntityName, Func<Type, string> GetEntityNameFromType)
{
    private readonly Dictionary<Type, TestableServiceBusProcessor> processors = new();
    private readonly Dictionary<Type, Mock<ServiceBusSender>> mockSenders = new();

    public void WireUpSendersAndProcessors(IServiceCollection services)
    {
        var client = new Mock<ServiceBusClient>();

        client.Setup(t => t.CreateProcessor(It.IsAny<string>(), It.IsAny<ServiceBusProcessorOptions>()))
            .Returns((string queue, ServiceBusProcessorOptions _) => GetProcessorByQueueName(queue));

        client.Setup(t => t.CreateSender(It.IsAny<string>()))
            .Returns<string>(GetSenderByQueueName);

        services.AddSingleton(client.Object);
    }

    public void AddProcessorFor<T>()
    {
        var processor = new TestableServiceBusProcessor(GetEntityNameFromType(typeof(T)));
        processors.Add(typeof(T), processor);
    }

    public bool HasProcessorFor<T>() => processors.ContainsKey(typeof(T));

    public TestableServiceBusProcessor GetProcessorFor<T>() => processors[typeof(T)];

    public TestableServiceBusProcessor GetProcessorFor(Type type) => processors[type];

    public TestableServiceBusProcessor GetProcessorByQueueName(string queueName)
    {
        var machineName = Environment.MachineName.ToLower();

        if (queueName.StartsWith(machineName))
            queueName = queueName.Replace($"{machineName}-", string.Empty);

        var typeName = GetTypeNameFromEntityName(queueName);

        foreach (var kvPair in processors)
        {
            if (kvPair.Key.Name == typeName)
                return kvPair.Value;
        }

        throw new Exception($"Can't find a registered TestableServiceBusProcessor for {queueName}");
    }

    public void ClearDeliveryAttemptsOnAllProcessors()
    {
        foreach (var processor in processors.Values)
        {
            processor.MessageDeliveryAttempts.Clear();
        }
    }

    public void ClearInvocationsOnAllSenders()
    {
        foreach (var sender in mockSenders.Values)
        {
            sender.Invocations.Clear();
        }
    }

    public void AddSenderFor<T>()
    {
        var mockSender = new Mock<ServiceBusSender>();
        mockSenders.Add(typeof(T), mockSender);
    }

    public Mock<ServiceBusSender>? GetSenderFor<T>()
    {
        var typeOfT = typeof(T);
        return mockSenders.ContainsKey(typeOfT) == false ? null : mockSenders[typeOfT];
    }

    public ServiceBusSender GetSenderByQueueName(string queueName)
    {
        var machineName = Environment.MachineName.ToLower();

        if (queueName.StartsWith(machineName))
            queueName = queueName.Replace($"{machineName}-", string.Empty);

        var typeName = GetTypeNameFromEntityName(queueName);

        foreach (var kvPair in mockSenders)
        {
            if (kvPair.Key.Name == typeName)
                return kvPair.Value.Object;
        }

        throw new Exception($"A Mock Sender was not found for the queue name {queueName}");
    }

    public void ClearAllInvocationsOnAllSenders()
    {
        foreach (var mockSender in mockSenders.Values)
        {
            mockSender.Invocations.Clear();
        }
    }

    public void MessagesSentToSenderWillBeReceivedOnCorrespondingProcessor<TMessageType>() where TMessageType : class
    {
        // If there is no MockSender for the given TMessageType then just return.
        var senderMock = GetSenderFor<TMessageType>();
        if (senderMock is null)
            return;

        // If there is no TestableServiceBusProcessor for the given TMessageType then just return.
        if (HasProcessorFor<TMessageType>() == false)
            return;

        senderMock.Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((sbm, ctx) =>
            {
                var message = sbm.Body.ToObjectFromJson<TMessageType>();
                var applicationProperties = (Dictionary<string, object>?)sbm.ApplicationProperties;

                if (message == null)
                    throw new Exception("unable to deserialise service bus message body");

                var processor = GetProcessorFor<TMessageType>();               

                processor.PresentMessage(message, applicationProperties: applicationProperties).GetAwaiter().GetResult();
            });
    }

    public void MessagesSentToSendersWillBeReceivedOnCorrespondingProcessors()
    {
        foreach (var mockSender in mockSenders)
        {
            if (processors.ContainsKey(mockSender.Key) == false)
                break;

            mockSender.Value.Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
                .Callback<ServiceBusMessage, CancellationToken>((sbm, ctx) =>
                {
                    var message = JsonSerializer.Deserialize(sbm.Body, mockSender.Key) ?? throw new Exception("unable to deserialise service bus message body");

                    var props = (Dictionary<string, object>?)sbm.ApplicationProperties;

                    var processor = GetProcessorFor(mockSender.Key);
                    processor.PresentMessage(message, applicationProperties: props).GetAwaiter().GetResult();
                });
        }
    }
}
