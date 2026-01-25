---
layout: default
---

# How? #8 Bending time⏲️

---
layout: default
---

# How? #8 Bending time⏲️

### TimeProvider

---
layout: default
---

# How? #8 Bending time⏲️

### TimeProvider 🗓️ .NET8

---
layout: default
---

# How? #8 Bending time⏲️

### TimeProvider _and_ FakeTimeProvider

---
layout: default
---

# How? #8 Bending time⏲️

```csharp
// From ComponentTestFixture...
FakeTimeProvider = new FakeTimeProvider();
FakeTimeProvider.SetUtcNow(TimeProvider.System.GetUtcNow());
FakeTimeProvider.AutoAdvanceAmount = TimeSpan.FromMilliseconds(100);
```

---
layout: default
---

# How? #8 Bending time⏲️

```csharp
// From ComponentTestFixture...
FakeTimeProvider = new FakeTimeProvider();
FakeTimeProvider.SetUtcNow(TimeProvider.System.GetUtcNow());
FakeTimeProvider.AutoAdvanceAmount = TimeSpan.FromMilliseconds(100); // slow down time!
```

---
layout: default
---

# How? #8 Bending time⏲️

```csharp
// in ComponentTestFixture...
FakeTimeProvider = new FakeTimeProvider();
FakeTimeProvider.SetUtcNow(TimeProvider.System.GetUtcNow());
FakeTimeProvider.AutoAdvanceAmount = TimeSpan.FromMilliseconds(100);
```

```csharp
// in each AppHostFactory, stick it into IoC, overriding the real one...
services.AddSingleton<TimeProvider>(fixture.FakeTimeProvider);
```
---
layout: default
---

# How? #8 Bending time⏲️

```csharp
// in OutboxDispatcherHostedService...
await Task.Delay(TimeSpan.FromSeconds(options.IntervalBetweenBatchesInSeconds), 
                    timeProvider, cancellationToken);
                    
await ProcessOutboxBatchAsync(options.BatchSize, cancellationToken);
```
---
layout: default
---

# How? #8 Bending time⏲️

```csharp
// in OutboxDispatcherHostedService...
await Task.Delay(TimeSpan.FromSeconds(options.IntervalBetweenBatchesInSeconds), 
                    timeProvider, cancellationToken);
// Time related methods now accept a TimeProvider
                    
await ProcessOutboxBatchAsync(options.BatchSize, cancellationToken);
```
---
layout: default
---

# How? #8 Bending time⏲️

```csharp
// in OutboxDispatcherHostedService...
await Task.Delay(TimeSpan.FromSeconds(options.IntervalBetweenBatchesInSeconds), 
                    timeProvider, cancellationToken);
                    
await ProcessOutboxBatchAsync(options.BatchSize, cancellationToken);

// but the timer will only advance by 100ms each time its checked...
// this code is waiting for 2 whole seconds!
```

---
layout: default
---

# How? #8 Bending time⏲️

```csharp
// then at the opportune moment...
public Then AfterSomeTimeHasPassed(int numberOfMsToAdvance = 2000)
{
    // Advance the time so the outbox processor wakes up to check for messages...
    fixture.FakeTimeProvider.Advance(TimeSpan.FromMilliseconds(numberOfMsToAdvance));
    // So cool!😁
}
```