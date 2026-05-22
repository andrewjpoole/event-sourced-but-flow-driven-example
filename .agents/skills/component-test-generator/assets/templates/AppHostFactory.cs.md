# AppHostFactory template

Purpose: show how to build `WebApplicationFactory<TProgram>` subclasses for each hosted app in the flow. Adapt environment-variable names, app names, repository interfaces, HTTP clients, and Service Bus entity configuration to the target solution.

## Pattern A — API factory

Use this for an ASP.NET Core API that receives HTTP requests and may call outbound HTTP services, but does not itself consume Service Bus messages.

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using {Namespace}.Infrastructure.ApiClients;
using {Namespace}.Infrastructure.Persistence;

namespace {Namespace}.Tests.TUnit.AppHostFactories;

public sealed class {ApiApp}WebApplicationFactory(ComponentTestFixture fixture)
    : WebApplicationFactory<{ApiApp}.Program>
{
    public HttpClient? HttpClient;
    public readonly Mock<ILogger> MockLogger = new();

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Point any service-discovery or options-based URLs at the fake base address.
        Environment.SetEnvironmentVariable("services__{external-service-1}__https__0", Constants.BaseUrl);
        Environment.SetEnvironmentVariable("services__{external-service-2}__https__0", Constants.BaseUrl);

        builder.ConfigureServices(services =>
        {
            services.AddMockLogger(MockLogger);
            services.AddSingleton<IEventRepository>(fixture.EventRepositoryInMemory);
            services.AddSingleton<TimeProvider>(fixture.FakeTimeProvider);

            services.AddHttpClient(typeof(I{ExternalService1}Client).FullName!, client => client.BaseAddress = new Uri(Constants.BaseUrl))
                .ConfigurePrimaryHttpMessageHandler(() => fixture.Mock{ExternalService1}HttpMessageHandler.Object);

            services.AddHttpClient(typeof(I{ExternalService2}Client).FullName!, client => client.BaseAddress = new Uri(Constants.BaseUrl))
                .ConfigurePrimaryHttpMessageHandler(() => fixture.Mock{ExternalService2}HttpMessageHandler.Object);
        });

        return base.CreateHost(builder);
    }

    public void Start() => HttpClient = CreateClient();
}
```

## Pattern B — EventListener / Worker factory

Use this for a background host that consumes Service Bus messages and usually persists events or writes to the outbox.

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using {Namespace}.Infrastructure.Messaging;
using {Namespace}.Infrastructure.Outbox;
using {Namespace}.Infrastructure.Persistence;

namespace {Namespace}.Tests.TUnit.AppHostFactories;

public sealed class {EventListenerApp}WebApplicationFactory(ComponentTestFixture fixture)
    : WebApplicationFactory<{EventListenerApp}.Program>
{
    public HttpClient? HttpClient;
    public readonly Mock<ILogger> MockLogger = new();

    protected override IHost CreateHost(IHostBuilder builder)
    {
        Environment.SetEnvironmentVariable($"{ServiceBusInboundOptions.SectionName}__{nameof(ServiceBusInboundOptions.InitialBackoffInMs)}", "2000");
        Environment.SetEnvironmentVariable($"{ServiceBusInboundOptions.SectionName}__{nameof(ServiceBusInboundOptions.MaxConcurrentCalls)}", "1");
        Environment.SetEnvironmentVariable($"{ServiceBusInboundOptions.SectionName}__{nameof(ServiceBusInboundOptions.Entities)}__{nameof(EntityNames.{InboundEvent1})}", EntityNames.{InboundEvent1});
        Environment.SetEnvironmentVariable($"{ServiceBusInboundOptions.SectionName}__{nameof(ServiceBusInboundOptions.Entities)}__{nameof(EntityNames.{InboundEvent2})}", EntityNames.{InboundEvent2});
        Environment.SetEnvironmentVariable($"{ServiceBusOutboundOptions.SectionName}__{nameof(ServiceBusOutboundOptions.Entities)}__{nameof(EntityNames.{OutboundEvent1})}", EntityNames.{OutboundEvent1});

        builder.ConfigureServices(services =>
        {
            services.AddMockLogger(MockLogger);
            services.AddSingleton<IEventRepository>(fixture.EventRepositoryInMemory);
            services.AddSingleton<IOutboxRepository>(fixture.OutboxRepositoryInMemory);
            services.AddSingleton<TimeProvider>(fixture.FakeTimeProvider);

            // Replace the real ServiceBusClient with the fake wiring.
            fixture.FakeServiceBus.WireUpSendersAndProcessors(services);
        });

        return base.CreateHost(builder);
    }

    public void Start() => HttpClient = CreateClient();
}
```

## Pattern C — Outbox / timer worker factory

Use this for a worker that periodically polls the outbox and publishes pending integration events.

```csharp
using System.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using {Namespace}.Infrastructure.Outbox;
using {Namespace}.Infrastructure.RetryableDapperConnection;

namespace {Namespace}.Tests.TUnit.AppHostFactories;

public sealed class {OutboxApp}WebApplicationFactory(ComponentTestFixture fixture)
    : WebApplicationFactory<{OutboxApp}.Program>
{
    public HttpClient? HttpClient;
    public readonly Mock<ILogger> MockLogger = new();

    protected override IHost CreateHost(IHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__{DatabaseName}", "dummy-connection-string");
        Environment.SetEnvironmentVariable($"{nameof(OutboxProcessorOptions)}__{nameof(OutboxProcessorOptions.IntervalBetweenBatchesInSeconds)}", "1");
        Environment.SetEnvironmentVariable($"{nameof(OutboxProcessorOptions)}__{nameof(OutboxProcessorOptions.InitialJitterSeconds)}", "0");

        builder.ConfigureServices(services =>
        {
            services.AddMockLogger(MockLogger);
            services.AddSingleton<IOutboxRepository>(fixture.OutboxRepositoryInMemory);
            services.AddSingleton<IOutboxBatchRepository>(fixture.OutboxRepositoryInMemory);
            services.AddSingleton<TimeProvider>(fixture.FakeTimeProvider);

            fixture.FakeServiceBus.WireUpSendersAndProcessors(services);
            ConfigureDatabaseConnectionFactory(services);
        });

        return base.CreateHost(builder);
    }

    public void Start() => HttpClient = CreateClient();

    private static void ConfigureDatabaseConnectionFactory(IServiceCollection services)
    {
        var mockTransaction = new Mock<IDbTransactionWrapped>();

        var mockConnection = new Mock<IRetryableConnection>();
        mockConnection
            .Setup(x => x.BeginTransaction(It.IsAny<IsolationLevel>()))
            .Returns(mockTransaction.Object);

        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        mockConnectionFactory.Setup(x => x.Create()).Returns(mockConnection.Object);

        services.AddSingleton(mockConnectionFactory.Object);
    }
}
```

Adaptation notes:

- Add or remove factory types based on the actual hosted apps discovered in `src/`.
- Always preserve the real startup path; only replace infrastructure seams.
- Every `Start()` method should call `CreateClient()` so the host boots in-process before the test acts against it.
