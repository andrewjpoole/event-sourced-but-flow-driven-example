using Microsoft.Extensions.DependencyInjection;
using WeatherApp.Application.Services;
using Microsoft.Extensions.Configuration;
using WeatherApp.Infrastructure.WeatherModelingSystem;

namespace WeatherApp.Infrastructure.ApiClients.WeatherModelingSystem;

public static class WeatherModelingServiceRegistrations
{
    public static IServiceCollection AddWeatherModelingService(this IServiceCollection services, IConfiguration config)
    {
        var options = config.GetSection(WeatherModelingServiceOptions.ConfigSectionName).Get<WeatherModelingServiceOptions>() ??
                                    new WeatherModelingServiceOptions();

        var serviceBaseUrl = config.GetValue<string>("services:weathermodelingservice:https:0");
        if (string.IsNullOrWhiteSpace(serviceBaseUrl))
            throw new Exception($"Expected a service url from aspire at services__weathermodelingservice__https__0.");

        var typeOfClientInterface = typeof(IWeatherModelingServiceClient);
        var nameOfClientInterface = typeOfClientInterface.FullName ?? typeOfClientInterface.Name;
        services.AddHttpClient(nameOfClientInterface, client =>
            {                
                //client.BaseAddress = new Uri(serviceBaseUrl);
                client.BaseAddress = new Uri("http+https://weathermodelingservice"); // using named endpoint via Aspire service discovery.
                client.DefaultRequestHeaders.Add(options.ApiManagerSubscriptionKeyHeader, options.SubscriptionKey);
            })
            .AddStandardResilienceHandler();

        services.AddSingleton<IWeatherModelingService, WeatherModelingService>();

        return services;
    }
}