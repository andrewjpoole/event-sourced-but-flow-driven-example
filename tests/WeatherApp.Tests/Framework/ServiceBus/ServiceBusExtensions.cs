namespace WeatherApp.Tests.Framework.ServiceBus;

public static class ServiceBusExtensions
{
    public const string DummyQueueNameSuffix = "-DummyQueueName";

    public static string GetDummyQueueName(this Type type) => $"{type.Name}{DummyQueueNameSuffix}";

    public static string GetTypeNameFromDummyQueueName(this string dummyQueueName) => dummyQueueName.Replace(DummyQueueNameSuffix, string.Empty);
}