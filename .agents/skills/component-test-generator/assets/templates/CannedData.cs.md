# CannedData template

Purpose: generate isolated, randomised test data and reusable event sequences for seeding event-sourced state. Adapt the event types, request models, value objects, and scenario builders to the target domain.

```csharp
using {Namespace}.Domain.DomainEvents;
using {Namespace}.Domain.EventSourcing;

namespace {Namespace}.Tests.TUnit;

public sealed class CannedData
{
    private readonly Random _random = new();

    public Guid StreamId { get; set; }
    public Guid RequestId { get; set; }
    public string Reference { get; set; }
    public string Location { get; set; }
    public string ExternalReference { get; set; }

    public CannedData(
        Guid? streamId = null,
        Guid? requestId = null,
        string? reference = null,
        string? location = null,
        string? externalReference = null)
    {
        StreamId = streamId ?? Guid.NewGuid();
        RequestId = requestId ?? Guid.NewGuid();
        Reference = reference ?? NewFruityReference();
        Location = location ?? $"location-{Guid.NewGuid():N}"[..20];
        ExternalReference = externalReference ?? $"ext-{Guid.NewGuid():N}"[..18];
    }

    public decimal RandomAmount() => decimal.Round((decimal)(_random.NextDouble() * 1000), 2);
    public int RandomQuantity() => _random.Next(1, 20);
    public DateTimeOffset RandomTimestamp() => DateTimeOffset.UtcNow.AddMinutes(_random.Next(-60, 60));

    // Domain-event factories. Replace placeholders with real event constructors.
    public {InitiatedEventType} {InitiatedEventType}(string? reference = null, string? idempotencyKey = null)
        => new(
            reference ?? Reference,
            idempotencyKey ?? RequestId.ToString());

    public {DomainEvent2} {DomainEvent2}()
        => new(StreamId, RandomTimestamp());

    public {DomainEvent3} {DomainEvent3}()
        => new(ExternalReference, RandomAmount());

    // Scenario builders seed an aggregate to a known point in time.
    public List<Event> UpTo_{InitiatedEventType}()
    {
        var version = 1;
        return new List<Event>
        {
            Event.Create({InitiatedEventType}(), StreamId, version++, null)
        };
    }

    public List<Event> UpTo_{DomainEvent3}Handled()
    {
        var version = 1;
        return new List<Event>
        {
            Event.Create({InitiatedEventType}(), StreamId, version++, null),
            Event.Create({DomainEvent2}(), StreamId, version++, null),
            Event.Create({DomainEvent3}(), StreamId, version++, null)
        };
    }

    public string NewFruityReference()
    {
        var fruits = new[] { "Apple", "Banana", "Cherry", "Date", "Fig", "Grape", "Kiwi", "Lime" };
        var fruit = fruits[Random.Shared.Next(fruits.Length)];
        var number = Random.Shared.Next(1000, 9999);
        return $"component-test-{fruit}{number}";
    }
}
```

How to extend it:

- Add one factory method per commonly used domain event.
- Add `UpTo_X` methods for every aggregate state you want to seed frequently.
- Prefer unique IDs and references per test run so component tests never interfere with one another.
