---
name: component-test-generator
description: >-
  Generates or extends in-process .NET component test scaffolds for event-sourced systems using WebApplicationFactory, Given/When/Then helpers, in-memory test infrastructure, and fake messaging/HTTP dependencies. Use it when you need a component test, end-to-end test, e2e test, or integration test scaffold for multiple hosted apps tested together in-process, especially with WebApplicationFactory, event-sourced flows, Given When Then DSLs, TUnit (preferred), or adaptations for xUnit/NUnit.
compatibility: Requires .NET repo with solution file. Works best with GitHub Copilot in agent mode.
---

# Overview

Use this skill to scaffold or extend in-process component tests for a .NET solution that runs multiple hosted apps together with `WebApplicationFactory<TProgram>`. The pattern replaces infrastructure with in-memory fakes and mocks so tests stay fast, deterministic, and readable while still exercising real startup, DI, middleware, handlers, repositories, messaging, and timer-driven behaviour.

## Workflow

### Step 1 — Discovery

Before generating anything, inspect the codebase and confirm the moving parts:

- Find the `.sln` file to understand the solution structure.
- Find all `Program.cs` files to identify hosted apps such as API, EventListener, Worker, Outbox, or scheduler processes.
- Search for domain event classes. Look for a shared base type/interface, or classes in a `DomainEvents` namespace/folder.
- Search for integration event classes, both inbound and outbound. Look for `IntegrationEvents` namespaces/folders and for `EntityNames` or Service Bus configuration that maps entity names to message types.
- Find external HTTP client interfaces by looking for `IXxxClient` interfaces and how they are registered in DI.
- Find `IEventRepository` or equivalent, plus `IOutboxRepository` / `IOutboxBatchRepository` if present.
- Check whether a component test project already exists by looking for `ComponentTests.cs` or test projects using `TUnit`, `xUnit`, or `NUnit` together with `WebApplicationFactory`.
- If stuck on any of the above, ask the user.

### Step 2 — Confirm with user

Before generating the scaffold, summarise what you found and ask the user to confirm:

1. The list of hosted apps that should be spun up in tests.
2. The name for the new test project, suggesting `{SolutionName}.Tests.TUnit`.
3. Which happy-path scenario should be implemented first, for example `submit data → receive confirmation`.
4. Any external HTTP services that were not detected automatically.
5. Whether an Outbox pattern is in use, if that is not already confirmed.

### Step 3 — Generate (first run)

Create the full test project and adapt every template to the target app. Create all of the following, substituting real namespaces, app names, event types, entity names, URLs, and repository interfaces:

- Test project `.csproj` based on `assets/templates/sample.csproj.md`
- `ComponentTestFixture.cs` based on `assets/templates/ComponentTestFixture.cs.md`
- One `WebApplicationFactory` subclass per hosted app based on `assets/templates/AppHostFactory.cs.md`
- `Given.cs`, `When.cs`, `Then.cs` based on `assets/templates/Given.cs.md`, `assets/templates/When.cs.md`, and `assets/templates/Then.cs.md`
- `CannedData.cs` based on `assets/templates/CannedData.cs.md`
- `FakeServiceBus.cs` and `TestableServiceBusProcessor.cs` based on `assets/templates/FakeServiceBus.cs.md` and `assets/templates/TestableServiceBusProcessor.cs.md`
- `EventRepositoryInMemory.cs` based on `assets/templates/EventRepositoryInMemory.cs.md`
- `OutboxRepositoryInMemory.cs` based on `assets/templates/OutboxRepositoryInMemory.cs.md` only if an outbox is detected
- `ServiceCollectionExtensions.cs` based on `assets/templates/ServiceCollectionExtensions.cs.md`
- `ComponentTests.cs` starter test based on `assets/templates/ComponentTests.cs.md`
- `Constants.cs` with the base URL and any discovered URI or entity-name constants

Also:

- Add the new test project to the solution file.
- Build the solution.
- Fix compile errors before finishing.

### Step 4 — Subsequent runs

When a component test project already contains `ComponentTests.cs`:

- Do **not** regenerate the whole scaffold.
- Ask the user which new scenario or edge case to add.
- Add new `Given` / `When` / `Then` methods as needed.
- Add new `CannedData` scenario methods as needed.
- Add the new test method(s) to `ComponentTests.cs` or create a new test file if that is clearer.

## Important notes

- Always use TUnit for new projects unless the user explicitly asks for xUnit or NUnit.
- Replace all infrastructure with in-memory fakes and mocks. Never use real databases, real Service Bus, or real HTTP services in component tests.
- The `FakeTimeProvider` pattern is essential for timer-driven behaviour such as outbox processing and retries.
- Keep the `Given` / `When` / `Then` DSL readable so test intent is obvious.
- Read the reference files for deeper detail before generating code.

## Reference files

- For the architecture pattern: `references/architecture.md`
- For discovery how-to: `references/discovery-guide.md`
