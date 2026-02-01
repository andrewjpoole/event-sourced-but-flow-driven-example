using System.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherApp.Infrastructure.Outbox;
using WeatherApp.Infrastructure.Persistence;
using WeatherApp.Infrastructure.RetryableDapperConnection;

namespace WeatherApp.Tests.TUnit.AppHostFactories;

public class OutboxApplicationFactory(ComponentTestFixture fixture) : WebApplicationFactory<Outbox.Program>
{    
    public HttpClient? HttpClient;
    public readonly Mock<ILogger> MockLogger = new();    
    
    protected override IHost CreateHost(IHostBuilder builder)
    {        
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__WeatherAppDb", "dummyConnectionString");
        Environment.SetEnvironmentVariable(
            $"{nameof(OutboxProcessorOptions)}__{nameof(OutboxProcessorOptions.IntervalBetweenBatchesInSeconds)}", "1");
        Environment.SetEnvironmentVariable(
            $"{nameof(OutboxProcessorOptions)}__{nameof(OutboxProcessorOptions.InitialJitterSeconds)}", "0");

        builder
            .ConfigureServices(services =>
            {
                services.AddMockLogger(MockLogger);

                services.AddSingleton<IOutboxRepository>(fixture.OutboxRepositoryInMemory);
                services.AddSingleton<IOutboxBatchRepository>(fixture.OutboxRepositoryInMemory);
                services.AddSingleton<TimeProvider>(fixture.FakeTimeProvider);
                
                fixture.FakeServiceBus.WireUpSendersAndProcessors(services);                

                ConfigureDatabaseConnectionFactory(services);
            });

        var host = base.CreateHost(builder);

        return host;
    }

    public void Start()
    {
        HttpClient = CreateClient();
    }

    private void ConfigureDatabaseConnectionFactory(IServiceCollection services)
    {
        var mockDbTransaction = new Mock<IDbTransactionWrapped>();

        var mockDbConnection = new Mock<IRetryableConnection>();
        mockDbConnection.Setup(x => x.BeginTransaction(It.IsAny<IsolationLevel>()))
            .Returns(mockDbTransaction.Object);
        
        var mockDbConnectionFactory = new Mock<IDbConnectionFactory>();
        mockDbConnectionFactory.Setup(x => x.Create()).Returns(mockDbConnection.Object);
        
        services.AddSingleton(mockDbConnectionFactory.Object);
    }
}