using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherApp.Infrastructure.ContributorPayments;
using WeatherApp.Infrastructure.Messaging;
using WeatherApp.Infrastructure.Outbox;
using WeatherApp.Infrastructure.Persistence;

namespace WeatherApp.Tests.TUnit.AppHostFactories;

public class EventListenerWebApplicationFactory(ComponentTestFixture fixture) : WebApplicationFactory<EventListener.Program>
{
    public readonly Mock<ILogger> MockLogger = new();
    
    public HttpClient? HttpClient;
        
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
                services.AddMockLogger(MockLogger);

                services.AddSingleton<IEventRepository>(fixture.EventRepositoryInMemory);
                services.AddSingleton<IOutboxRepository>(fixture.OutboxRepositoryInMemory);
                services.AddSingleton<TimeProvider>(fixture.FakeTimeProvider);

                services.AddHttpClient(typeof(IContributorPaymentServiceClient).FullName!, client => client.BaseAddress = new Uri(Constants.BaseUrl))
                    .ConfigurePrimaryHttpMessageHandler(() => fixture.MockContributorPaymentsServiceHttpMessageHandler.Object);

                fixture.FakeServiceBus.WireUpSendersAndProcessors(services);
                
            });

        var host = base.CreateHost(builder);

        return host;
    }    
    
    public void Start()
    {
        HttpClient = CreateClient();
    }
}