namespace WeatherApp.Infrastructure.Messaging;

public class QueueOrTopicName(string queueOrTopicName)
{    public string Name { get; } = queueOrTopicName;
}