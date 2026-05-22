# Architecture reference

Purpose: explain the in-process component testing pattern that this skill should reproduce and adapt for a target .NET solution.

## 1. Philosophy

In-process component tests give you most of the confidence of an end-to-end test without the cost and flakiness of spinning up real infrastructure. The real application startup code still runs, so you exercise middleware, routing, model binding, validation, dependency injection, handlers, repositories, background services, message consumers, and serialization paths through production code.

The trick is to replace infrastructure seams with deterministic fakes:

- persistence becomes in-memory repositories
- Azure Service Bus becomes a fake bus and directly injectable processors
- external HTTP calls become `Mock<HttpMessageHandler>` instances
- time becomes a controllable `FakeTimeProvider`

That gives you tests that are fast, hermetic, reproducible, and capable of covering multi-step flows across multiple hosted apps.

## 2. ComponentTestFixture

`ComponentTestFixture` is the hub for one test case. Create a fresh instance per test to guarantee isolation.

It typically holds:

- one `WebApplicationFactory<TProgram>` subclass per hosted app
- `EventRepositoryInMemory`, shared by all app hosts
- `OutboxRepositoryInMemory`, shared when the app uses an outbox pattern
- one `Mock<HttpMessageHandler>` per external HTTP dependency
- `FakeServiceBus`, containing fake processors and senders
- `FakeTimeProvider`, used by timer-based workers and retry logic
- a `SetupHelpers()` method returning `(Given, When, Then, CannedData)`
- `Dispose()` logic that cleans up `HttpClient` instances and other disposables

This central fixture makes it easy for `Given`, `When`, and `Then` helpers to operate against the same in-memory world.

## 3. WebApplicationFactory subclasses

Create one `WebApplicationFactory<TProgram>` subclass per hosted app, for example API, EventListener, Outbox, or other workers.

Each factory should:

- extend `WebApplicationFactory<TProgram>`
- override `CreateHost(IHostBuilder builder)`
- call `builder.ConfigureServices(...)` to replace production registrations with test doubles
- register shared in-memory repositories from the fixture
- wire mock `HttpMessageHandler` instances into the same named or typed `HttpClient` registrations used by production code
- wire `FakeServiceBus` into `ServiceBusClient`, `ServiceBusSender`, and `ServiceBusProcessor` resolution when the host consumes or publishes messages
- set required environment variables with `Environment.SetEnvironmentVariable`
- expose a `Start()` helper that calls `CreateClient()` to force host startup

The important insight is that `WebApplicationFactory` lets the real app boot in-process. You are not unit testing controllers or handlers in isolation; you are exercising real startup with fake edges.

## 4. Given / When / Then

The DSL is built from three classes, all holding a reference to the fixture.

### Given

Responsibilities usually include:

- starting hosts
- clearing state between phases
- seeding persisted events
- configuring mock HTTP responses
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

## 8. In-memory fakes vs mocks

Persistence components are better as in-memory fakes than pure mocks.

- `EventRepositoryInMemory` and `OutboxRepositoryInMemory` implement the real interfaces and store state in memory
- this allows real query behaviour and direct assertions against stored state
- external HTTP dependencies are still best mocked with `Mock<HttpMessageHandler>` and `Moq.Contrib.HttpClient`

This combination gives you realistic behaviour where it matters without depending on external systems.

## 9. Multi-phase test pattern

Complex scenarios often need several phases:

1. send an HTTP request
2. assert immediate state
3. inject a Service Bus message
4. assert the next state
5. advance fake time
6. assert the final state

Use `InPhase("1 (description)")`, `InPhase("2 (description)")`, and so on so diagnostics point to the right step. Use `FakeTimeProvider.Advance(TimeSpan)` instead of real delays whenever timers or periodic polling drive the next transition.
