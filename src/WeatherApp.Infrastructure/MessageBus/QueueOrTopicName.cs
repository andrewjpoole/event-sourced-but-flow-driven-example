namespace WeatherApp.Infrastructure.MessageBus;

public class QueueOrTopicName
{
    public string Name { get; }
    public QueueOrTopicName(string queueOrTopicName)
    {
#if DEBUG
        Name = $"{Environment.MachineName.ToLower()}-{queueOrTopicName}";
#else
        Name = queueOrTopicName;
#endif
    }
}