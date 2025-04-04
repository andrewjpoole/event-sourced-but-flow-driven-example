using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherApp.Application.Models.IntegrationEvents.WeatherModelingEvents;
using WeatherApp.Infrastructure.Persistence;
using WeatherApp.Tests.Framework.ServiceBus;

namespace WeatherApp.Tests.AppHostFactories;

public class OutboxApplicationFactory(ComponentTestFixture fixture) : WebApplicationFactory<Outbox.Program>
{
    private readonly ComponentTestFixture fixture = fixture;
    public HttpClient? HttpClient;

    public readonly Mock<ILogger> MockLogger = new();
    
    //public Func<EventRepositoryInMemory>? SetSharedEventRepository = null;

    // Using CreateHost here instead of ConfigureWebHost because CreateHost adds config just after WebApplication.CreateBuilder(args) is called
    // whereas ConfigureWebHost is called too late just before builder.Build() is called.
    protected override IHost CreateHost(IHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("ServiceBus__Outbound__Names__ModelingDataAcceptedIntegrationEvent", typeof(DummyIntegrationEvent).GetDummyQueueName());                
        Environment.SetEnvironmentVariable("ServiceBusSettings__FullyQualifiedNamespace", "component-test-servicebus-namespace");
        Environment.SetEnvironmentVariable("ConnectionStrings__WeatherAppDb", "dummyConnectionString");

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