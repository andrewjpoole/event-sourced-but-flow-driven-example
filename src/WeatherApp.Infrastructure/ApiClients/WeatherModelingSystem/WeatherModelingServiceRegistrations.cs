using Microsoft.Extensions.DependencyInjection;
using WeatherApp.Application.Services;
using WeatherApp.Infrastructure.ModelingService;
using Polly;

namespace WeatherApp.Infrastructure.ApiClients.WeatherModelingSystem;

public static class WeatherModelingServiceRegistrations
{
    public static IServiceCollection AddWeatherModelingService(this IServiceCollection services, WeatherModelingServiceOptions options)
    {
        var typeOfClientInterface = typeof(WeatherModelingService);
        var nameOfClientInterface = typeOfClientInterface.FullName ?? typeOfClientInterface.Name;
        services.AddHttpClient(nameOfClientInterface, client =>
            {
                if (string.IsNullOrWhiteSpace(options.BaseUrl))
                    throw new Exception($"{nameof(options.BaseUrl)} is required on {nameof(WeatherModelingServiceOptions)} section in config.");

                client.BaseAddress = new Uri(options.BaseUrl);
                client.DefaultRequestHeaders.Add(options.ApiManagerSubscriptionKeyHeader, options.SubscriptionKey);
            })
            .AddTransientHttpErrorPolicy(policy =>
                policy.WaitAndRetryAsync(options.MaxRetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

        services.AddSingleton<IWeatherModelingService, WeatherModelingService>();

        return services;
    }
}