using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherApp.Infrastructure.Outbox;

namespace WeatherApp.Tests.AppHostFactories;

public class OutboxApplicationFactory(ComponentTestFixture fixture) : WebApplicationFactory<Outbox.Program>
{
    private readonly ComponentTestFixture fixture = fixture;
    public HttpClient? HttpClient;

    public readonly Mock<ILogger> MockLogger = new();
       

    // Using CreateHost here instead of ConfigureWebHost because CreateHost adds config just after WebApplication.CreateBuilder(args) is called
    // whereas ConfigureWebHost is called too late just before builder.Build() is called.
    protected override IHost CreateHost(IHostBuilder builder)
    {        
        Environment.SetEnvironmentVariable("ConnectionStrings__WeatherAppDb", "dummyConnectionString");
        Environment.SetEnvironmentVariable($"{nameof(OutboxProcessorOptions)}__{nameof(OutboxProcessorOptions.IntervalBetweenBatchesInSeconds)}", "15");

        builder
            .ConfigureServices(services =>
            {
                var loggerFactory = new Mock<ILoggerFactory>();
                loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);
                services.AddSingleton(loggerFactory.Object);

                fixture.MockServiceBus.WireUpSendersAndProcessors(services);

                // ToDo add mocked or inMemory db here?

                // if (SetSharedEventRepository is not null)
                //     services.AddSingleton<IEventRepository>(_ => SetSharedEventRepository());
            });

        var host = base.CreateHost(builder);

        return host;
    }

    public void Start()
    {
        HttpClient = CreateClient();
    }
}