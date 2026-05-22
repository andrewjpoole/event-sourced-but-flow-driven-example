# AppHostFactory template

Purpose: show how to build `WebApplicationFactory<TProgram>` subclasses for each hosted app in the flow. Adapt app names, repository interfaces, HTTP clients, and Service Bus entity configuration to the target solution.

> **`extern alias` note**: if multiple apps in the solution declare `Program` with no namespace, add `<Aliases>global,{AppAlias}</Aliases>` to each `<ProjectReference>` in the test `.csproj` and use `extern alias {AppAlias};` at the top of each factory file with `{AppAlias}::Program` as the type argument.

## Pattern A — API factory

Use this for an ASP.NET Core API that receives HTTP requests and may call outbound HTTP services, but does not itself consume Service Bus messages.

```csharp
extern alias ApiAlias;

using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using {Namespace}.Infrastructure.Persistence;

namespace {Namespace}.Tests.TUnit.AppHostFactories;

public sealed class {ApiApp}WebApplicationFactory : WebApplicationFactory<ApiAlias::{ApiApp}.Program>
{
    private readonly ComponentTestFixture _fixture;

    public HttpClient? HttpClient;
    public readonly Mock<ILogger> MockLogger = new();

    public {ApiApp}WebApplicationFactory(ComponentTestFixture fixture)
    {
        _fixture = fixture;
        // Bind on a real dynamic port — available in Microsoft.AspNetCore.Mvc.Testing 10.0.0+.
        // Do NOT use ListenLocalhost(0); dynamic port binding on localhost is not supported.
        UseKestrel(x => x.Listen(IPAddress.Loopback, 0));
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Override service-discovery URL keys to point at the fake server.
        // Prefer ConfigureAppConfiguration over Environment.SetEnvironmentVariable
        // to avoid cross-test pollution.
        builder.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["services__{external-service-1}__https__0"] = _fixture.FakeExternalServicesServer.BaseUrl,
                ["services__{external-service-2}__https__0"] = _fixture.FakeExternalServicesServer.BaseUrl,
            }));

        builder.ConfigureServices(services =>
        {
            services.AddMockLogger(MockLogger);
            services.AddSingleton<IEventRepository>(_fixture.EventRepositoryInMemory);
            services.AddSingleton<TimeProvider>(_fixture.FakeTimeProvider);
        });

        return base.CreateHost(builder);
    }

    public void Start()
    {
        HttpClient = CreateClient();
        var address = Server.Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>()!
            .Addresses.First();
        Console.WriteLine($"[Given] {typeof({ApiApp}WebApplicationFactory).Name.Replace("WebApplicationFactory", "")} started → {address}");
    }
}
```


## Pattern B — EventListener / Worker factory

Use this for a background host that consumes Service Bus messages and usually persists events or writes to the outbox.

```csharp
extern alias EventListenerAlias;

using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using {Namespace}.Infrastructure.Messaging;
using {Namespace}.Infrastructure.Outbox;
using {Namespace}.Infrastructure.Persistence;

namespace {Namespace}.Tests.TUnit.AppHostFactories;

public sealed class {EventListenerApp}WebApplicationFactory : WebApplicationFactory<EventListenerAlias::{EventListenerApp}.Program>
{
    private readonly ComponentTestFixture _fixture;

    public HttpClient? HttpClient;
    public readonly Mock<ILogger> MockLogger = new();

    public {EventListenerApp}WebApplicationFactory(ComponentTestFixture fixture)
    {
        _fixture = fixture;
        UseKestrel(x => x.Listen(IPAddress.Loopback, 0));
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{ServiceBusInboundOptions.SectionName}__{nameof(ServiceBusInboundOptions.InitialBackoffInMs)}"] = "2000",
                [$"{ServiceBusInboundOptions.SectionName}__{nameof(ServiceBusInboundOptions.MaxConcurrentCalls)}"] = "1",
                [$"{ServiceBusInboundOptions.SectionName}__{nameof(ServiceBusInboundOptions.Entities)}__{nameof(EntityNames.{InboundEvent1})}"] = EntityNames.{InboundEvent1},
                // If the EventListener also calls external services, override their URLs here too:
                // ["services__{external-service}__https__0"] = _fixture.FakeExternalServicesServer.BaseUrl,
            }));

        builder.ConfigureServices(services =>
        {
            services.AddMockLogger(MockLogger);
            services.AddSingleton<IEventRepository>(_fixture.EventRepositoryInMemory);
            services.AddSingleton<IOutboxRepository>(_fixture.OutboxRepositoryInMemory);
            services.AddSingleton<TimeProvider>(_fixture.FakeTimeProvider);

            _fixture.FakeServiceBus.WireUpSendersAndProcessors(services);
        });

        return base.CreateHost(builder);
    }

    public void Start()
    {
        HttpClient = CreateClient();
        Console.WriteLine($"[Given] EventListener started → {Server.Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>()!.Addresses.First()}");
    }
}
```

## Pattern C — Outbox / timer worker factory

Use this for a worker that periodically polls the outbox and publishes pending integration events.

```csharp
extern alias OutboxAlias;

using System.Data;
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using {Namespace}.Infrastructure.Outbox;
using {Namespace}.Infrastructure.RetryableDapperConnection;

namespace {Namespace}.Tests.TUnit.AppHostFactories;

public sealed class {OutboxApp}WebApplicationFactory : WebApplicationFactory<OutboxAlias::{OutboxApp}.Program>
{
    private readonly ComponentTestFixture _fixture;

    public HttpClient? HttpClient;
    public readonly Mock<ILogger> MockLogger = new();

    public {OutboxApp}WebApplicationFactory(ComponentTestFixture fixture)
    {
        _fixture = fixture;
        UseKestrel(x => x.Listen(IPAddress.Loopback, 0));
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings__{DatabaseName}"] = "dummy-connection-string",
                [$"{nameof(OutboxProcessorOptions)}__{nameof(OutboxProcessorOptions.IntervalBetweenBatchesInSeconds)}"] = "1",
                [$"{nameof(OutboxProcessorOptions)}__{nameof(OutboxProcessorOptions.InitialJitterSeconds)}"] = "0",
            }));

        builder.ConfigureServices(services =>
        {
            services.AddMockLogger(MockLogger);
            services.AddSingleton<IOutboxRepository>(_fixture.OutboxRepositoryInMemory);
            services.AddSingleton<IOutboxBatchRepository>(_fixture.OutboxRepositoryInMemory);
            services.AddSingleton<TimeProvider>(_fixture.FakeTimeProvider);

            _fixture.FakeServiceBus.WireUpSendersAndProcessors(services);
            ConfigureDatabaseConnectionFactory(services);
        });

        return base.CreateHost(builder);
    }

    public void Start()
    {
        HttpClient = CreateClient();
        Console.WriteLine($"[Given] Outbox started → {Server.Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>()!.Addresses.First()}");
    }

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
- If the solution has only one app (no namespace collisions on `Program`), drop `extern alias` and use the plain `Program` type directly.
