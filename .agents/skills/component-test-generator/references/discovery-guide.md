# Discovery guide

Purpose: help an agent inspect an unfamiliar .NET codebase and gather the information needed to scaffold component tests correctly.

## 1. Solution structure

Start with the solution file:

```text
glob **/*.sln
```

Then inspect the projects referenced by the `.sln` and note which belong under `src/` and which belong under `tests/`.

## 2. Hosted applications

Search for application entry points:

```text
glob src/**/Program.cs
```

Also inspect `.csproj` files for web or executable apps:

- `Microsoft.NET.Sdk.Web`
- `<OutputType>Exe</OutputType>`

For each hosted app, classify it:

- ASP.NET API: usually uses `WebApplication.CreateBuilder`, `MapGet`, `MapPost`, controllers, or minimal APIs
- Worker service: often has `BackgroundService`, `IHostedService`, or queue processing
- timer/outbox worker: often polls on an interval and pushes outbound messages

These are the apps most likely to need their own `WebApplicationFactory<TProgram>` subclass.

## 3. Domain events

Look for a domain event folder or namespace:

```text
glob src/**/DomainEvents/*.cs
```

If that fails, search for likely base types:

```text
grep -r "IDomainEvent\|: DomainEvent\|: Event" src/ --include="*.cs" -l
```

Domain events in event-sourced systems are normally immutable records or classes describing past-tense facts such as `OrderPlaced`, `PaymentConfirmed`, or `WeatherRequested`.

## 4. Integration events

Look for inbound and outbound transport messages:

```text
glob src/**/IntegrationEvents/**/*.cs
```

Also search for entity-name configuration or queue/topic mapping:

```text
grep -r "EntityNames\|entityName\|TopicName\|QueueName" src/ --include="*.cs" -l
```

Capture both directions:

- inbound messages that workers or listeners consume
- outbound messages that APIs or workers publish

You need the entity-name-to-type mapping to configure `FakeServiceBus` correctly.

## 5. External HTTP clients

Find client-like interfaces:

```text
grep -r "interface I.*Client\|interface I.*Service" src/ --include="*.cs" -l
```

Then find DI registration:

```text
grep -r "AddHttpClient" src/ --include="*.cs" -n
```

The registration often reveals:

- typed client type
- named client string
- base address setup
- any custom headers or delegating handlers

That information is needed when overriding the registration in the test host.

## 6. Event and outbox repositories

Search for persistence seams:

```text
grep -r "IEventRepository\|IOutboxRepository\|IOutboxBatchRepository" src/ --include="*.cs" -l
```

Open the interfaces and note the exact methods. The in-memory fake should implement the real contract closely, not a guessed subset.

## 7. Service Bus configuration

Search for options or configuration classes:

```text
grep -r "ServiceBusInboundOptions\|ServiceBusOutboundOptions\|ServiceBusProcessorOptions" src/ --include="*.cs" -l
```

Also inspect environment variable names, queue/topic options, subscriptions, and entity-name mapping logic. Confirm whether topology is simple queues or something more complex.

## 8. Existing test framework

Check test projects:

```text
glob tests/**/*.csproj
```

Look for packages such as:

- `TUnit`
- `xunit`
- `NUnit`
- `Microsoft.NET.Test.Sdk`
- `Microsoft.AspNetCore.Mvc.Testing`

Also search for `ComponentTests.cs`. If it already exists, treat the task as an extension run rather than a first scaffold.

## 9. DI registration of infrastructure

Read infrastructure registration methods because they reveal everything that needs replacing:

```text
grep -r "AddSingleton\|AddScoped\|AddTransient" src/ --include="*.cs" -n | grep -i "repository\|client\|service\|bus"
```

Also look for methods like `AddInfrastructure()`, `AddPersistence()`, or `AddMessaging()`.

## When to ask the user

Ask for clarification when:

- you cannot find a clear domain event base type or folder
- entity-name-to-type mapping is not obvious
- more than three external HTTP services appear and you need to confirm which belong in the first scenario
- Service Bus topology looks complex, for example subscriptions plus multiple topics
- there is no obvious single happy-path flow visible from endpoints, handlers, or current docs
