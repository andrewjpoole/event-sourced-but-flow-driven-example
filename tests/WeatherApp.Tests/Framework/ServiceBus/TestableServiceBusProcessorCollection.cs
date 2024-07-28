namespace WeatherApp.Tests.Framework.ServiceBus;

public class TestableServiceBusProcessorCollection
{
    private readonly Dictionary<Type, TestableServiceBusProcessor> processors = new();

    public void AddProcessorFor<T>()
    {
        var processor = new TestableServiceBusProcessor(typeof(T).GetDummyQueueName());
        processors.Add(typeof(T), processor);
    }

    public bool HasProcessorFor<T>() => processors.ContainsKey(typeof(T));

    public TestableServiceBusProcessor GetProcessorFor<T>() => processors[typeof(T)];

    public TestableServiceBusProcessor? GetByDummyQueueName(string dummyQueueName, bool prefixLocalMachineName)
    {
        foreach (var processor in processors.Values)
        {
            var prefixedDummyQueueName = prefixLocalMachineName ? $"{Environment.MachineName}-{processor.DummyQueueName}" : processor.DummyQueueName;
            if (prefixedDummyQueueName == dummyQueueName)
                return processor;
        }

        return null;
    }

    public void ClearDeliveryAttemptsOnAllProcessors()
    {
        foreach (var processor in processors.Values)
        {
            processor.MessageDeliveryAttempts.Clear();
        }
    }
}