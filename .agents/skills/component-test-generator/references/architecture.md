# Architecture reference

Purpose: explain the in-process component testing pattern that this skill should reproduce and adapt for a target .NET solution.

## 1. Philosophy

In-process component tests give you most of the confidence of an end-to-end test without the cost and flakiness of spinning up real infrastructure. The real application startup code still runs, so you exercise middleware, routing, model binding, validation, dependency injection, handlers, repositories, background services, message consumers, and serialization paths through production code.

The trick is to replace infrastructure seams with deterministic fakes:

- persistence becomes in-memory repositories
- Azure Service Bus becomes a fake bus and directly injectable processors
- external HTTP calls go to a real `FakeExternalServicesServer` (a real Kestrel `WebApplication` on a dynamic port)
- time becomes a controllable `FakeTimeProvider`

That gives you tests that are fast, hermetic, reproducible, and capable of covering multi-step flows across multiple hosted apps.

## 2. ComponentTestFixture

`ComponentTestFixture` is the hub for one test case. Create a fresh instance per test to guarantee isolation.

It typically holds:

- one `WebApplicationFactory<TProgram>` subclass per hosted app
- `EventRepositoryInMemory`, shared by all app hosts
- `OutboxRepositoryInMemory`, shared when the app uses an outbox pattern
- a `FakeExternalServicesServer` that listens on a real dynamic port for external HTTP calls
- `FakeServiceBus`, containing fake processors and senders
- `FakeTimeProvider`, used by timer-based workers and retry logic
- a `SetupHelpers()` method returning `(Given, When, Then, CannedData)`
- `Dispose()` logic that cleans up `HttpClient` instances and other disposables

This central fixture makes it easy for `Given`, `When`, and `Then` helpers to operate against the same in-memory world.

## 3. WebApplicationFactory subclasses

Create one `WebApplicationFactory<TProgram>` subclass per hosted app, for example API, EventListener, Outbox, or other workers.

Each factory should:

- extend `WebApplicationFactory<TProgram>`
- call `UseKestrel(x => x.Listen(IPAddress.Loopback, 0))` **in the constructor** to bind on a dynamic port (available in `Microsoft.AspNetCore.Mvc.Testing` 10.0.0+)
  - **Do not** use `ListenLocalhost(0)` — dynamic port binding on localhost is not supported at runtime
- override `CreateHost(IHostBuilder builder)`
- call `builder.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(...))` to override service-discovery URLs so they point at the `FakeExternalServicesServer` port
  - Prefer `ConfigureAppConfiguration` over `Environment.SetEnvironmentVariable` to avoid cross-test pollution
- call `builder.ConfigureServices(...)` to replace production registrations with test doubles
- register shared in-memory repositories from the fixture
- wire `FakeServiceBus` into `ServiceBusClient`, `ServiceBusSender`, and `ServiceBusProcessor` resolution when the host consumes or publishes messages
- expose a `Start()` helper that calls `CreateClient()` to force host startup

The important insight is that `WebApplicationFactory` lets the real app boot in-process. You are not unit testing controllers or handlers in isolation; you are exercising real startup with fake edges.

## 4. Given / When / Then

The DSL is built from three classes, all holding a reference to the fixture.

### Given

Responsibilities usually include:

- starting hosts
- clearing state between phases
- seeding persisted events
- setting up fake external service state (e.g. `fixture.FakeExternalServicesServer.State.SeatAvailable = true`)
- priming fake senders or processors

### When

Responsibilities usually include:

- sending HTTP requests to the API app
- wrapping payloads in request messages
- presenting Service Bus messages directly to a processor
- advancing time for timer-based workers
- tracking a phase label for clearer diagnostics

### Then

Responsibilities usually include:

- asserting HTTP status codes and response bodies
- checking persisted domain events
- asserting sent integration messages
- verifying that inbound messages were handled
- checking outbox inserts or updates
- performing retry-based assertions for async behaviour

Both `When` and `Then` benefit from `InPhase(string)` so failure messages can say which stage of a multi-phase scenario failed.

Polly retry in `Then` is important because asynchronous background processing often completes just after the triggering action. A small retry wrapper keeps tests deterministic without arbitrary sleeps.

## 5. CannedData

`CannedData` generates isolated test data per test run.

Typical responsibilities:

- generate unique stream IDs, request IDs, references, locations, or external IDs
- provide factory methods for concrete domain events
- provide scenario builders such as `UpTo_X()` that return `List<Event>` for seeding state
- encode event sourcing conventions such as `Event.Create(domainEvent, streamId, version, null)`

This keeps tests short and avoids repetitive, brittle object construction.

## 6. FakeServiceBus

`FakeServiceBus` replaces Azure Service Bus in component tests.

It should provide:

- `AddProcessorFor<T>()` to register a `TestableServiceBusProcessor` for an inbound type
- `AddSenderFor<T>()` to register a `Mock<ServiceBusSender>` for an outbound type
- `WireUpSendersAndProcessors(IServiceCollection)` to register a mocked `ServiceBusClient`
- `MessagesSentToSendersWillBeReceivedOnCorrespondingProcessors()` so published messages can loop back into processors for round-trip flow testing
- mapping functions for `entityName -> typeName` and `type -> entityName`

That mapping must match the app's own configuration logic, often via an `EntityNames` class or Service Bus options.

## 7. TestableServiceBusProcessor

`TestableServiceBusProcessor` extends `ServiceBusProcessor` so tests can inject messages directly.

Expected behaviour:

- `PresentMessage<T>(message)` simulates delivery to the registered message handler
- `PresentMessageWithRetries<T>(message, maxDeliveryCount)` can simulate multiple deliveries
- `StartProcessingAsync` should do nothing so no real polling occurs
- `MessageDeliveryAttempts` tracks all deliveries for later assertions

A matching `TestableMessageEventArgs` type can wrap `ProcessMessageEventArgs` and record whether a message was completed or dead-lettered.

## 8. FakeExternalServicesServer

For external HTTP dependencies (services your app calls outward), use a real `FakeExternalServicesServer` rather than `Mock<HttpMessageHandler>`.

Why a real server is preferred over mock handlers:

- The app's `HttpClient` registrations (named or typed) configure base addresses via service-discovery configuration keys. Overriding those keys to point at the fake server means the real `HttpClient` pipeline runs unchanged — no `ConfigurePrimaryHttpMessageHandler` fighting with DI wiring.
- Route parameters, query strings, and request bodies are visible in the fake handler, making it easy to log and assert exactly what the app sent.
- `Console.WriteLine` in endpoint handlers provides per-request evidence in the test output.

Implementation pattern:

```csharp
// FakeServicesState.cs — simple boolean flags, one per controllable behaviour
public class FakeServicesState
{
    public bool SeatAvailable { get; set; } = true;
    public bool PaymentAccepted { get; set; } = true;
    public void Reset() { SeatAvailable = true; PaymentAccepted = true; }
}

// FakeExternalServicesServer.cs — real Kestrel WebApplication
public sealed class FakeExternalServicesServer : IDisposable
{
    private readonly WebApplication _app;
    public readonly FakeServicesState State = new();
    public string BaseUrl { get; private set; } = string.Empty;

    public FakeExternalServicesServer()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(o => o.Listen(IPAddress.Loopback, 0));
        builder.Logging.ClearProviders();
        _app = builder.Build();
        MapEndpoints(_app);
        _app.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

        var address = _app.Services
            .GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.First();
        BaseUrl = address;
        Console.WriteLine($"[FakeServer] Listening on {BaseUrl}");
    }

    private void MapEndpoints(WebApplication app)
    {
        app.MapPost("/seats/reserve/{ticketRef}", (string ticketRef) =>
        {
            var status = State.SeatAvailable
                ? Results.Ok(new { ticketRef })
                : Results.UnprocessableEntity(new { reason = "no seats" });
            Console.WriteLine($"[FakeServer] POST seats/reserve/{ticketRef} → {(State.SeatAvailable ? 200 : 422)}");
            return status;
        });

        // Add one endpoint per external service operation
    }

    public void Dispose() => _app.DisposeAsync().AsTask().GetAwaiter().GetResult();
}
```

Key details:

- Use `builder.WebHost.ConfigureKestrel(...)`, **not** `builder.WebHost.UseKestrel(...)` — on `WebApplicationBuilder`, Kestrel is configured via `ConfigureKestrel`.
- Use `Listen(IPAddress.Loopback, 0)` for a dynamic port. **Do not** use `ListenLocalhost(0)` — dynamic port binding on localhost throws a runtime exception.
- Read the actual bound port from `IServerAddressesFeature` after `StartAsync`.
- Create `FakeExternalServicesServer` **before** the app factories in `ComponentTestFixture` so the port is known when factories configure URL overrides.
- Dispose it in `ComponentTestFixture.Dispose()` after the app factories.

## 9. In-memory persistence fakes

Persistence components are better as in-memory fakes than pure mocks.

- `EventRepositoryInMemory` and `OutboxRepositoryInMemory` implement the real interfaces and store state in memory.
- This allows real query behaviour and direct assertions against stored state.


## 10. Multi-phase test pattern

Complex scenarios often need several phases:

1. send an HTTP request
2. assert immediate state
3. inject a Service Bus message
4. assert the next state
5. advance fake time
6. assert the final state

Use `InPhase("1 (description)")`, `InPhase("2 (description)")`, and so on so diagnostics point to the right step. Use `FakeTimeProvider.Advance(TimeSpan)` instead of real delays whenever timers or periodic polling drive the next transition.

## 11. Test output logging

TUnit (and MTP-based runners) capture `Console.WriteLine` output per test and display it when a test passes or fails. Add structured log lines throughout the DSL so the output tells a story without requiring a debugger.

Recommended conventions:

- `[Given]` prefix in `Given` methods — log state set, events seeded, servers started with their bound addresses
- `[When]` prefix in `When` methods — log HTTP method + URL before sending, status code on response, injected message type and key fields
- `[Then]` prefix in `Then` methods — log each assertion with a `✓` on pass
- `[FakeServer]` prefix in `FakeExternalServicesServer` endpoint handlers — log method, route params, and response status for every inbound call from the apps under test

Example output from a passing test:

```
[Given] SeatAllocationService → will return 200 (seat available)
[Given] API started → http://127.0.0.1:54321/
[When] → POST v1/bookings/CONF-abc/attendee-xyz
[FakeServer] POST seats/reserve/TKT-001 (conf=CONF-abc) → 200 OK
[When] ← 200 OK
[Then] ✓ Response status = 200 OK
[Then] ✓ Domain event persisted: TicketBookingInitiated
```

To see this output when running via `dotnet run`:

```
dotnet run --project tests/YourProject -- --output Detailed --no-progress
```

The `dotnet test` command with TUnit/MTP does not display per-test output by default; use `dotnet run` with the arguments above to get readable results.

### `extern alias` for multiple `Program` classes

When the solution contains several apps that each declare `public partial class Program` with no namespace, the test project cannot reference all of them as `Program` without ambiguity. Fix this by assigning `<Aliases>` in the `.csproj` project references:

```xml
<ProjectReference Include="..\..\src\BookingSystem.API\BookingSystem.API.csproj">
  <Aliases>global,ApiAlias</Aliases>
</ProjectReference>
<ProjectReference Include="..\..\src\BookingSystem.EventListener\BookingSystem.EventListener.csproj">
  <Aliases>global,EventListenerAlias</Aliases>
</ProjectReference>
```

Then in each factory file, add `extern alias ApiAlias;` at the top and use `ApiAlias::Program` as the type argument to `WebApplicationFactory<T>`.

