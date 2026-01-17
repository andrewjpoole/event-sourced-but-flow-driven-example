using System.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherApp.Infrastructure.Outbox;
using WeatherApp.Infrastructure.Persistence;
using WeatherApp.Infrastructure.RetryableDapperConnection;
using WeatherApp.Tests.TUnit.Framework.Persistence;

namespace WeatherApp.Tests.TUnit.AppHostFactories;

public class OutboxApplicationFactory(ComponentTestFixture fixture) : WebApplicationFactory<Outbox.Program>
{    
    public HttpClient? HttpClient;
    public readonly Mock<ILogger> MockLogger = new();
    public Func<OutboxRepositoryInMemory>? SetSharedOutboxRepositories = null;
    
    protected override IHost CreateHost(IHostBuilder builder)
    {        
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__WeatherAppDb", "dummyConnectionString");
        Environment.SetEnvironmentVariable(
            $"{nameof(OutboxProcessorOptions)}__{nameof(OutboxProcessorOptions.IntervalBetweenBatchesInSeconds)}", "1");

        builder
            .ConfigureServices(services =>
            {
                services.AddMockLogger(MockLogger);
                services.AddSingleton<TimeProvider>(fixture.FakeTimeProvider);
                
                fixture.FakeServiceBus.WireUpSendersAndProcessors(services);

                if (SetSharedOutboxRepositories is not null)
                {
                    var combinedOutboxAndBatchRepository = SetSharedOutboxRepositories();
                    services.AddSingleton<IOutboxRepository>(_ => combinedOutboxAndBatchRepository);
                    services.AddSingleton<IOutboxBatchRepository>(_ => combinedOutboxAndBatchRepository);
                }

                var mockDbTransaction = new Mock<IDbTransactionWrapped>();
                var mockDbConnection = new Mock<IRetryableConnection>();
                mockDbConnection.Setup(x => x.BeginTransaction(It.IsAny<IsolationLevel>()))
                    .Returns(mockDbTransaction.Object);
                var mockDbConnectionFactory = new Mock<IDbConnectionFactory>();
                mockDbConnectionFactory.Setup(x => x.Create()).Returns(mockDbConnection.Object);
                services.AddSingleton(mockDbConnectionFactory.Object);
            });

        var host = base.CreateHost(builder);

        return host;
    }

    public void Start()
    {
        HttpClient = CreateClient();
    }
}