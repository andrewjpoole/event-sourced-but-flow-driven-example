namespace WeatherApp.Infrastructure.MessageBus;

public interface IMessageSender<T>
{
    Task SendAsync(T message, CancellationToken cancellationToken = default);
}
