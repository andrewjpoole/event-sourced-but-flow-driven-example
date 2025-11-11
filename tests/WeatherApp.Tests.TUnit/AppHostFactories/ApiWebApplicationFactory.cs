using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherApp.Infrastructure.ApiClients.WeatherModelingSystem;
using WeatherApp.Infrastructure.ContributorPayments;
using WeatherApp.Infrastructure.Persistence;
using WeatherApp.Tests.TUnit.Framework.Persistence;

namespace WeatherApp.Tests.TUnit.AppHostFactories;

public class ApiWebApplicationFactory(ComponentTestFixture fixture) : WebApplicationFactory<API.Program>
{
    public HttpClient? HttpClient;
    public readonly Mock<ILogger> MockLogger = new();
    public readonly Mock<HttpMessageHandler> MockWeatherModelingServiceHttpMessageHandler = new(MockBehavior.Strict);

    public Func<EventRepositoryInMemory>? SetSharedEventRepository = null;
    
    protected override IHost CreateHost(IHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("services__contributorpaymentsservice__https__0", Constants.BaseUrl);
        Environment.SetEnvironmentVariable("services__weathermodelingservice__https__0", Constants.BaseUrl);

        builder
            .ConfigureServices(services =>
            {
                services.AddMockLogger(MockLogger);

                services.AddSingleton<TimeProvider>(fixture.FakeTimeProvider);

                services.AddHttpClient(typeof(IContributorPaymentServiceClient).FullName!, client => client.BaseAddress = new Uri(Constants.BaseUrl))
                    .ConfigurePrimaryHttpMessageHandler(() => fixture.MockContributorPaymentsServiceHttpMessageHandler.Object);

                services.AddHttpClient(typeof(IWeatherModelingServiceClient).FullName!, client => client.BaseAddress = new Uri(Constants.BaseUrl))
                    .ConfigurePrimaryHttpMessageHandler(() => MockWeatherModelingServiceHttpMessageHandler.Object);

                if (SetSharedEventRepository is not null)
                    services.AddSingleton<IEventRepository>(_ => SetSharedEventRepository());
               
            });

        var host = base.CreateHost(builder);

        return host;
    }

    public void Start()
    {
        HttpClient = CreateClient();
    }
}
