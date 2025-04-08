namespace WeatherApp.Infrastructure.MessageBus;

public interface IUniversalMessageSender
{    
    Task SendAsync(string serializedPayload, string entityName, CancellationToken cancellationToken = default);
}
