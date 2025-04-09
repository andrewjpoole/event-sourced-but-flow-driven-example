using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeatherApp.Application.Services;

namespace WeatherApp.Infrastructure.ContributorPayments;

public static class ContributorPaymentsServiceRegistrations
{
    public static IServiceCollection AddContributorPaymentsService(this IServiceCollection services, IConfiguration config)
    {
        var options = config.GetSection(ContributorPaymentServiceOptions.ConfigSectionName).Get<ContributorPaymentServiceOptions>() ??
                                    new ContributorPaymentServiceOptions();

        var serviceBaseUrl = config.GetValue<string>("services:contributorpaymentsservice:https:0");
        if (string.IsNullOrWhiteSpace(serviceBaseUrl))
            throw new Exception($"Expected a service url from aspire at services__contributorpaymentsservice__https__0.");

        var typeOfClientInterface = typeof(IContributorPaymentServiceClient);
        var nameOfClientInterface = typeOfClientInterface.FullName ?? typeOfClientInterface.Name;
        services.AddHttpClient(nameOfClientInterface, client =>
            {
                //client.BaseAddress = new Uri(serviceBaseUrl);
                client.BaseAddress = new("http+https://contributorpaymentsservice"); // using named endpoint via Aspire service discovery.
                client.DefaultRequestHeaders.Add(options.ApiManagerSubscriptionKeyHeader, options.SubscriptionKey);
            })
            .AddStandardResilienceHandler();

        services.AddSingleton<IContributorPaymentService, ContributorPaymentService>();

        return services;
    }
}
