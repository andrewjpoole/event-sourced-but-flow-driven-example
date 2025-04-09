namespace WeatherApp.Infrastructure.Messaging;

public interface IUniversalMessageSender
{    
    Task SendAsync(string serializedPayload, string entityName, CancellationToken cancellationToken = default);
}
