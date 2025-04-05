namespace WeatherApp.Infrastructure.ApiClientWrapper;

public interface IRefitClientWrapper<out T>
{
    T CreateClient();
}