# component-test-generator

An [Agent Skill](https://agentskills.io) that scaffolds in-process component tests for event-sourced .NET solutions.

Point it at any .NET repo that follows the multi-app, event-sourced pattern and it will:

1. **Discover** the solution structure — hosted apps, domain events, integration events, external HTTP clients, Service Bus topology, repository interfaces
2. **Confirm** its findings with you and ask a few setup questions
3. **Generate** a complete new test project: `ComponentTestFixture`, `Given`/`When`/`Then` DSL, `WebApplicationFactory` per app, in-memory fakes, `FakeServiceBus`, `CannedData`, and a starter `ComponentTests.cs` with a happy-path test
4. **Extend** on subsequent runs — add new scenario tests guided by you without regenerating the whole scaffold

All infrastructure is replaced with in-memory fakes so tests are fast, deterministic, and require no real databases, Service Bus, or HTTP services.

## Installation

### Windows (PowerShell)

```powershell
# Clone or open the repo containing the skill, then:
.\.agents\skills\component-test-generator\install.ps1
```

### Mac / Linux (bash)

```bash
./.agents/skills/component-test-generator/install.sh
```

The script creates a symlink at `~/.agents/skills/component-test-generator` pointing to the skill in this repo. Edits to the skill files are immediately reflected everywhere — no re-install needed.

## Installer options

| Command | Description |
|---|---|
| `.\install.ps1` | Install via symlink (default) |
| `.\install.ps1 -Copy` | Install by copying files (safe if the repo moves) |
| `.\install.ps1 update` | Re-create symlink / re-copy (e.g. after moving the repo) |
| `.\install.ps1 uninstall` | Remove the installed skill |
| `.\install.ps1 install -Location <path>` | Install to a custom directory |

Bash equivalents use `--copy` and `--location`. Both scripts are idempotent.

## Usage

Once installed, open a .NET repo in your agent client (e.g. VS Code in Copilot agent mode) and ask:

> *"create component tests for this app"*

or run `/skills` first to confirm `component-test-generator` is listed.

On subsequent runs in a repo that already has `ComponentTests.cs`, the skill switches to extend mode and asks which new scenario to add.

## What gets generated

```
{SolutionName}.Tests.TUnit/
├── {SolutionName}.Tests.TUnit.csproj
├── ComponentTestFixture.cs
├── ComponentTests.cs
├── CannedData.cs
├── Constants.cs
├── AppHostFactories/
│   ├── {ApiApp}WebApplicationFactory.cs
│   ├── {EventListenerApp}WebApplicationFactory.cs
│   ├── {OutboxApp}WebApplicationFactory.cs      ← only if outbox detected
│   └── ServiceCollectionExtensions.cs
└── Framework/
    ├── Given.cs
    ├── When.cs
    ├── Then.cs
    ├── Persistence/
    │   ├── EventRepositoryInMemory.cs
    │   └── OutboxRepositoryInMemory.cs           ← only if outbox detected
    └── ServiceBus/
        ├── FakeServiceBus.cs
        └── TestableServiceBusProcessor.cs
```

## Skill structure

```
component-test-generator/
├── SKILL.md                          ← skill entry point (loaded by agent)
├── install.ps1                       ← Windows installer
├── install.sh                        ← Mac/Linux installer
├── references/
│   ├── architecture.md               ← deep explanation of the testing pattern
│   └── discovery-guide.md            ← how to analyse an unfamiliar .NET repo
└── assets/
    └── templates/                    ← generic annotated C# templates
        ├── ComponentTestFixture.cs.md
        ├── AppHostFactory.cs.md
        ├── Given.cs.md
        ├── When.cs.md
        ├── Then.cs.md
        ├── CannedData.cs.md
        ├── FakeServiceBus.cs.md
        ├── TestableServiceBusProcessor.cs.md
        ├── EventRepositoryInMemory.cs.md
        ├── OutboxRepositoryInMemory.cs.md
        ├── ComponentTests.cs.md
        ├── ServiceCollectionExtensions.cs.md
        └── sample.csproj.md
```

## Preferred test framework

[TUnit](https://tunit.dev) — recommended for new projects. The skill can detect and target xUnit or NUnit if already present.
