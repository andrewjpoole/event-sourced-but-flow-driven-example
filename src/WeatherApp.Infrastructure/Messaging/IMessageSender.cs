namespace WeatherApp.Infrastructure.Messaging;

public interface IMessageSender<T>
{
    Task SendAsync(T message, CancellationToken cancellationToken = default);
}
