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

        var serviceBaseUrl = config.GetValue<string>("services:weathermodelingservice:https:0");
        if (string.IsNullOrWhiteSpace(serviceBaseUrl))
            throw new Exception($"Expected a service url from aspire at services__weathermodelingservice__https__0.");

        var typeOfClientInterface = typeof(IWeatherModelingServiceClient);
        var nameOfClientInterface = typeOfClientInterface.FullName ?? typeOfClientInterface.Name;
        services.AddHttpClient(nameOfClientInterface, client =>
            {                
                client.BaseAddress = new Uri(serviceBaseUrl);
                client.DefaultRequestHeaders.Add(options.ApiManagerSubscriptionKeyHeader, options.SubscriptionKey);
            })
            .AddTransientHttpErrorPolicy(policy =>
                policy.WaitAndRetryAsync(options.MaxRetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

        services.AddSingleton<IWeatherModelingService, WeatherModelingService>();

        return services;
    }
}