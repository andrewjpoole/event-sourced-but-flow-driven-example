using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using WeatherApp.Application.Services;

namespace WeatherApp.Infrastructure.ContributorPayments;

public static class ContributorPaymentsServiceRegistrations
{
    public static IServiceCollection AddContributorPaymentsService(this IServiceCollection services, IConfiguration config)
    {
        var options = config.GetSection(ContributorPaymentServiceOptions.ConfigSectionName).Get<ContributorPaymentServiceOptions>() ??
                                    throw new Exception($"A {nameof(ContributorPaymentServiceOptions)} config section is required.");

        var serviceBaseUrl = config.GetValue<string>("services:contributorpaymentsservice:https:0");
        if (string.IsNullOrWhiteSpace(serviceBaseUrl))
            throw new Exception($"Expected a service url from aspire at services__weathermodelingservice__https__0.");

        var typeOfClientInterface = typeof(IContributorPaymentServiceClient);
        var nameOfClientInterface = typeOfClientInterface.FullName ?? typeOfClientInterface.Name;
        services.AddHttpClient(nameOfClientInterface, client =>
            {
                client.BaseAddress = new Uri(serviceBaseUrl);
                client.DefaultRequestHeaders.Add(options.ApiManagerSubscriptionKeyHeader, options.SubscriptionKey);
            })
            .AddTransientHttpErrorPolicy(policy =>
                policy.WaitAndRetryAsync(options.MaxRetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

        services.AddSingleton<IContributorPaymentService, ContributorPaymentService>();

        return services;
    }
}
