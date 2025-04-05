using Microsoft.Extensions.DependencyInjection;
using WeatherApp.Application.Services;
using Polly;
using Microsoft.Extensions.Configuration;
using WeatherApp.Infrastructure.WeatherModelingSystem;

namespace WeatherApp.Infrastructure.ApiClients.WeatherModelingSystem;

public static class WeatherModelingServiceRegistrations
{
    public static IServiceCollection AddWeatherModelingService(this IServiceCollection services, IConfiguration config)
    {
        var options = config.GetSection(WeatherModelingServiceOptions.ConfigSectionName).Get<WeatherModelingServiceOptions>() ??
                                    throw new Exception($"A {nameof(WeatherModelingServiceOptions)} config section is required.");

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