using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using WeatherApp.Application.Services;
using WeatherApp.Infrastructure.ApiClients.NotificationService;
using WeatherApp.Infrastructure.ContributorPayments;

namespace WeatherApp.Infrastructure.ApiClients.ContributorPaymentsService;

public static class ContributorPaymentsServiceRegistrations
{
    public static IServiceCollection AddContributorPaymentsService(this IServiceCollection services, IConfiguration config)
    {
        var options = config.GetSection(ContributorPaymentServiceOptions.ConfigSectionName).Get<ContributorPaymentServiceOptions>() ??
                                    throw new Exception($"A {nameof(ContributorPaymentServiceOptions)} config section is required.");

        var typeOfClientInterface = typeof(IContributorPaymentServiceClient);
        var nameOfClientInterface = typeOfClientInterface.FullName ?? typeOfClientInterface.Name;
        services.AddHttpClient(nameOfClientInterface, client =>
            {
                if (string.IsNullOrWhiteSpace(options.BaseUrl))
                    throw new Exception($"{nameof(options.BaseUrl)} is required on {nameof(ContributorPaymentsServiceRegistrations)} section in config.");

                client.BaseAddress = new Uri(options.BaseUrl);
                client.DefaultRequestHeaders.Add(options.ApiManagerSubscriptionKeyHeader, options.SubscriptionKey);
            })
            .AddTransientHttpErrorPolicy(policy =>
                policy.WaitAndRetryAsync(options.MaxRetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

        services.AddSingleton<IContributorPaymentService, ContributorPaymentService>();

        return services;
    }
}
