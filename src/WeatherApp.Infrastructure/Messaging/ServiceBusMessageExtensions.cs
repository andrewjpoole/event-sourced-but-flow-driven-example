using System.Text.Json;
using Azure.Messaging.ServiceBus;
using WeatherApp.Domain.EventSourcing;

namespace WeatherApp.Infrastructure.Messaging;

public static class ServiceBusMessageExtensions
{
    public static T? GetJsonPayload<T>(this ServiceBusReceivedMessage message)
    {
        return JsonSerializer.Deserialize<T>(message.Body.ToString(), GlobalJsonSerialiserSettings.Default);
    }
}