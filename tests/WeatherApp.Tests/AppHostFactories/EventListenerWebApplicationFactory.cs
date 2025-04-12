using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherApp.Infrastructure.ContributorPayments;
using WeatherApp.Infrastructure.Messaging;
using WeatherApp.Infrastructure.Outbox;
using WeatherApp.Infrastructure.Persistence;
using WeatherApp.Tests.e2eComponentTests.Framework.Persistence;

namespace WeatherApp.Tests.AppHostFactories;

public class EventListenerWebApplicationFactory(ComponentTestFixture fixture) : WebApplicationFactory<EventListener.Program>
{
    private readonly ComponentTestFixture fixture = fixture;    

    public readonly Mock<ILogger> MockLogger = new();
    public Func<EventRepositoryInMemory>? SetSharedEventRepository = null;
    public Func<OutboxRepositoryInMemory>? SetSharedOutboxRepositories = null;
    public HttpClient? HttpClient;
    
    // Using CreateHost here instead of ConfigureWebHost because CreateHost adds config just after WebApplication.CreateBuilder(args) is called
    // whereas ConfigureWebHost is called too late just before builder.Build() is called
    protected override IHost CreateHost(IHostBuilder builder)
    {        
        Environment.SetEnvironmentVariable($"{ServiceBusInboundOptions.SectionName}__{nameof(ServiceBusInboundOptions.InitialBackoffInMs)}", "2000");
        Environment.SetEnvironmentVariable($"{ServiceBusInboundOptions.SectionName}__{nameof(ServiceBusInboundOptions.MaxConcurrentCalls)}", "1");
        Environment.SetEnvironmentVariable($"{ServiceBusInboundOptions.SectionName}__{nameof(ServiceBusInboundOptions.Entities)}__{nameof(EntityNames.ModelingDataAcceptedIntegrationEvent)}", EntityNames.ModelingDataAcceptedIntegrationEvent);
        Environment.SetEnvironmentVariable($"{ServiceBusInboundOptions.SectionName}__{nameof(ServiceBusInboundOptions.Entities)}__{nameof(EntityNames.ModelingDataRejectedIntegrationEvent)}", EntityNames.ModelingDataRejectedIntegrationEvent);
        Environment.SetEnvironmentVariable($"{ServiceBusInboundOptions.SectionName}__{nameof(ServiceBusInboundOptions.Entities)}__{nameof(EntityNames.ModelUpdatedIntegrationEvent)}", EntityNames.ModelUpdatedIntegrationEvent);
    
        Environment.SetEnvironmentVariable($"{ServiceBusOutboundOptions.SectionName}__{nameof(ServiceBusOutboundOptions.Entities)}__{nameof(EntityNames.UserNotificationEvent)}", EntityNames.UserNotificationEvent);

        builder
            .ConfigureServices(services =>
            {
                var loggerFactory = new Mock<ILoggerFactory>();
                loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);
                services.AddSingleton(loggerFactory.Object);

                services.AddSingleton<TimeProvider>(fixture.FakeTimeProvider);

                services.AddHttpClient(typeof(IContributorPaymentServiceClient).FullName!, client => client.BaseAddress = new Uri(Constants.BaseUrl))
                    .ConfigurePrimaryHttpMessageHandler(() => fixture.MockContributorPaymentsServiceHttpMessageHandler.Object);

                fixture.MockServiceBus.WireUpSendersAndProcessors(services);
                
                if (SetSharedEventRepository is not null)
                    services.AddSingleton<IEventRepository>(_ => SetSharedEventRepository());

                if (SetSharedOutboxRepositories is not null)
                {
                    var combinedOutboxAndBatchRepository = SetSharedOutboxRepositories();
                    services.AddSingleton<IOutboxRepository>(_ => combinedOutboxAndBatchRepository);
                    services.AddSingleton<IOutboxBatchRepository>(_ => combinedOutboxAndBatchRepository);
                }
            });

        var host = base.CreateHost(builder);

        return host;
    }    
    
    public void Start()
    {
        HttpClient = CreateClient();
    }
}