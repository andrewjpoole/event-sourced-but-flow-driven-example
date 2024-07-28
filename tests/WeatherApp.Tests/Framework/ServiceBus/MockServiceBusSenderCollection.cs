using Azure.Messaging.ServiceBus;
using Moq;

namespace WeatherApp.Tests.Framework.ServiceBus;

public class MockServiceBusSenderCollection
{
    private readonly Dictionary<Type, Mock<ServiceBusSender>> mockSenders = new();

    public void AddSenderFor<T>()
    {
        var mockSender = new Mock<ServiceBusSender>();
        mockSenders.Add(typeof(T), mockSender);
    }

    public Mock<ServiceBusSender> GetSenderFor<T>()
    {
        return mockSenders[typeof(T)];
    }

    public ServiceBusSender GetByDummyQueueName(string dummyQueueName, bool prefixLocalMachineName)
    {
        var machineName = Environment.MachineName;

        if (dummyQueueName.StartsWith(machineName))
            dummyQueueName = dummyQueueName.Replace($"{machineName}-", string.Empty);

        var typeName = dummyQueueName.GetTypeNameFromDummyQueueName();

        foreach (var kvPair in mockSenders)
        {
            if (kvPair.Key.Name == typeName)
                return kvPair.Value.Object;
        }

        throw new Exception($"A Mock Sender was not found for the dummy queue name {dummyQueueName}");
    }

    public void ClearAllInvocations()
    {
        foreach (var mockSender in mockSenders.Values)
        {
            mockSender.Invocations.Clear();
        }
    }
}