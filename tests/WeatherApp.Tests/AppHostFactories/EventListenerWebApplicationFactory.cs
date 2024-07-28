using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherApp.Application.Models.IntegrationEvents.WeatherModelingEvents;
using WeatherApp.Infrastructure.Persistence;
using WeatherApp.Tests.Framework.ServiceBus;

namespace WeatherApp.Tests.AppHostFactories;

public class EventListenerWebApplicationFactory : WebApplicationFactory<EventListener.Program>
{
    private readonly CustomHttpClientFactory httpClientFactory = new();
    public TestableServiceBusProcessorCollection TestableServiceBusProcessors { get; }
    public int NumberOfSimulatedServiceBusMessageRetries = 3;
    public readonly Mock<ILogger> MockLogger = new();

    public Func<EventRepositoryInMemory>? SetSharedEventRepository = null;

    public HttpClient? HttpClient;

#if DEBUG
    private const bool DebugCompilationSymbolIsPresent = true;
#else
    private const bool DebugCompilationSymbolIsPresent = false;
#endif
    
    public EventListenerWebApplicationFactory()
    {
        TestableServiceBusProcessors = new TestableServiceBusProcessorCollection();
        TestableServiceBusProcessors.AddProcessorFor<ModelingDataAcceptedIntegrationEvent>();
        TestableServiceBusProcessors.AddProcessorFor<ModelingDataRejectedIntegrationEvent>();
        TestableServiceBusProcessors.AddProcessorFor<ModelUpdatedIntegrationEvent>();
    }
    
    // Using CreateHost here instead of ConfigureWebHost because CreateHost adds config just after WebApplication.CreateBuilder(args) is called
    // whereas ConfigureWebHost is called too late just before builder.Build() is called
    protected override IHost CreateHost(IHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("ServiceBus__Inbound__Names__ModelingDataAcceptedIntegrationEvent", typeof(ModelingDataAcceptedIntegrationEvent).GetDummyQueueName());
        Environment.SetEnvironmentVariable("ServiceBus__Inbound__Names__ModelingDataRejectedIntegrationEvent", typeof(ModelingDataRejectedIntegrationEvent).GetDummyQueueName());
        Environment.SetEnvironmentVariable("ServiceBus__Inbound__Names__ModelUpdatedIntegrationEvent", typeof(ModelUpdatedIntegrationEvent).GetDummyQueueName());
        Environment.SetEnvironmentVariable("ServiceBus__Inbound__MaxConcurrentCalls", "1");
        Environment.SetEnvironmentVariable("ServiceBus__Inbound__InitialBackoffInMs", "2000");
        Environment.SetEnvironmentVariable("ServiceBus__Inbound__PrefetchCount", "1");
        Environment.SetEnvironmentVariable("ServiceBusSettings__FullyQualifiedNamespace", "component-test-servicebus-namespace");

        Environment.SetEnvironmentVariable("NotificationsServiceOptions__BaseUrl", Constants.WeatherModelingServiceBaseUrl); // Value will not be used but does need to be a valid URI.
        Environment.SetEnvironmentVariable("NotificationsServiceOptions__MaxRetryCount", "3");

        builder
            .ConfigureServices(services =>
            {
                var loggerFactory = new Mock<ILoggerFactory>();
                loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);
                services.AddSingleton(loggerFactory.Object);

                var client = new Mock<ServiceBusClient>();
                client.Setup(t => t.CreateProcessor(It.IsAny<string>(), It.IsAny<ServiceBusProcessorOptions>())).Returns((string queue, ServiceBusProcessorOptions _) =>
                {
                    return TestableServiceBusProcessors.GetByDummyQueueName(queue, DebugCompilationSymbolIsPresent) ?? 
                           throw new Exception($"Can't find a registered TestableServiceBusProcessor for {queue}");

                    return null!;
                });
                services.AddSingleton(client.Object);

                if (SetSharedEventRepository is not null)
                    services.AddSingleton<IEventRepository>(_ => SetSharedEventRepository());
            });

        var host = base.CreateHost(builder);

        return host;
    }
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            // Replace standard IHttpClientFactory impl with custom one with any added HTTP clients.
            services.AddSingleton<IHttpClientFactory>(httpClientFactory);
        });
    }

    public void ClearHttpClients() => httpClientFactory.HttpClients.Clear();

    public void AddHttpClient(string clientName, HttpClient client)
    {
        if (httpClientFactory.HttpClients.TryAdd(clientName, client) == false)
        {
            throw new InvalidOperationException($"HttpClient with name {clientName} is already added");
        }
    }

    public void Start()
    {
        HttpClient = CreateClient();
    }
}