namespace WeatherApp.Application.Services;

public interface IEventHandler<in T> where T : class
{
    Task HandleEvent(T @event);
}