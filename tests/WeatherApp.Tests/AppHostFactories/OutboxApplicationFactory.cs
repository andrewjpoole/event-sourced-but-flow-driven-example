using System.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherApp.Infrastructure.Outbox;
using WeatherApp.Infrastructure.Persistence;
using WeatherApp.Infrastructure.RetryableDapperConnection;
using WeatherApp.Tests.e2eComponentTests.Framework.Persistence;

namespace WeatherApp.Tests.AppHostFactories;

public class OutboxApplicationFactory(ComponentTestFixture fixture) : WebApplicationFactory<Outbox.Program>
{
    private readonly ComponentTestFixture fixture = fixture;
    public HttpClient? HttpClient;
    public readonly Mock<ILogger> MockLogger = new();

    public Func<OutboxRepositoryInMemory>? SetSharedOutboxRepositories = null;

    // Using CreateHost here instead of ConfigureWebHost because CreateHost adds config just after WebApplication.CreateBuilder(args) is called
    // whereas ConfigureWebHost is called too late just before builder.Build() is called.
    protected override IHost CreateHost(IHostBuilder builder)
    {        
        Environment.SetEnvironmentVariable("ConnectionStrings__WeatherAppDb", "dummyConnectionString");
        Environment.SetEnvironmentVariable($"{nameof(OutboxProcessorOptions)}__{nameof(OutboxProcessorOptions.IntervalBetweenBatchesInSeconds)}", "1");

        builder
            .ConfigureServices(services =>
            {
                var loggerFactory = new Mock<ILoggerFactory>();
                loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);
                services.AddSingleton(loggerFactory.Object);

                services.AddSingleton<TimeProvider>(fixture.FakeTimeProvider);

                fixture.MockServiceBus.WireUpSendersAndProcessors(services);

                if (SetSharedOutboxRepositories is not null)
                {
                    var combinedOutboxAndBatchRepository = SetSharedOutboxRepositories();
                    services.AddSingleton<IOutboxRepository>(_ => combinedOutboxAndBatchRepository);
                    services.AddSingleton<IOutboxBatchRepository>(_ => combinedOutboxAndBatchRepository);
                }

                var mockDbTransaction = new Mock<IDbTransactionWrapped>();
                var mockDbConnection = new Mock<IRetryableConnection>();
                mockDbConnection.Setup(x => x.BeginTransaction(It.IsAny<IsolationLevel>())).Returns(mockDbTransaction.Object);
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