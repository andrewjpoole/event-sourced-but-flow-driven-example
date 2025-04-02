namespace WeatherApp.Infrastructure.MessageBus;

public interface IUniversalMessageSender
{
    Task SendAsync(object payload, string entityName, CancellationToken cancellationToken = default);
    Task SendAsync(string serializedPayload, string entityName, CancellationToken cancellationToken = default);
}
