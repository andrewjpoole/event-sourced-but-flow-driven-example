# Then template

Purpose: hold readable assertions for HTTP responses, persisted events, Service Bus handling, and outbox behaviour. Adapt the initiated event type, external endpoint assertions, and persistence model names for the target app.

```csharp
using System.Net;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Moq;
using Moq.Contrib.HttpClient;
using Polly;
using Shouldly;
using {Namespace}.Domain.EventSourcing;
using {Namespace}.Domain.DomainEvents;

namespace {Namespace}.Tests.TUnit.Framework;

public sealed class Then(ComponentTestFixture fixture)
{
    public Then And => this;

    public Then AndAlso(string name, Action assertion)
    {
        assertion();
        return this;
    }

    public Then InPhase(string newPhase)
    {
        fixture.SetPhase(newPhase);
        return this;
    }

    public Then TheResponseCodeShouldBe(HttpResponseMessage response, HttpStatusCode code)
    {
        response.ShouldNotBeNull();
        response.StatusCode.ShouldBe(code, $"{fixture.CurrentPhase}expected that the response code was {code}.");
        return this;
    }

    public Then TheBodyShouldBeEmpty(HttpResponseMessage response)
    {
        var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        body.ShouldBeEmpty($"{fixture.CurrentPhase}expected that the response body would be empty.");
        return this;
    }

    public Then TheBodyShouldNotBeEmpty(HttpResponseMessage response, out string body)
    {
        body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        body.ShouldNotBeEmpty($"{fixture.CurrentPhase}expected that the response body would not be empty.");
        return this;
    }

    public Then TheBodyShouldNotBeEmpty(HttpResponseMessage response)
    {
        TheBodyShouldNotBeEmpty(response, out _);
        return this;
    }

    public Then TheBodyShouldNotBeEmpty(HttpResponseMessage response, Action<string>? assertAgainstBody = null)
    {
        TheBodyShouldNotBeEmpty(response, out var body);
        assertAgainstBody?.Invoke(body);
        return this;
    }

    public Then TheBodyShouldNotBeEmpty<T>(HttpResponseMessage response, out T bodyAsT)
    {
        TheBodyShouldNotBeEmpty(response, out var body);

        try
        {
            bodyAsT = JsonSerializer.Deserialize<T>(body, GlobalJsonSerialiserSettings.Default)
                ?? throw new InvalidOperationException("Deserialised body was null.");
        }
        catch (Exception ex)
        {
            throw new Exception(
                $"{fixture.CurrentPhase}Unable to deserialise response body into {typeof(T).Name}. Body: {body}",
                ex);
        }

        return this;
    }

    public Then TheBodyShouldNotBeEmpty<T>(HttpResponseMessage response, Action<T>? assertAgainstBody = null)
    {
        TheBodyShouldNotBeEmpty<T>(response, out var bodyAsT);
        assertAgainstBody?.Invoke(bodyAsT);
        return this;
    }

    public Then The{ExternalService1}EndpointShouldHaveBeenCalled(int times = 1)
    {
        fixture.Mock{ExternalService1}HttpMessageHandler.VerifyRequest(
            HttpMethod.Post,
            request => request.RequestUri!.ToString().StartsWith($"{Constants.BaseUrl}{Constants.{ExternalService1}UriStart}"),
            Times.Exactly(times),
            $"{fixture.CurrentPhase}expected the {ExternalService1} endpoint to have been called {times} time(s).");

        return this;
    }

    public Then The{ExternalService1}EndpointShouldNotHaveBeenCalled() =>
        The{ExternalService1}EndpointShouldHaveBeenCalled(0);

    public Then TheDomainEventShouldHaveBeenPersisted<T>()
    {
        var eventClassName = typeof(T).FullName ?? typeof(T).Name;
        var found = fixture.EventRepositoryInMemory.PersistedEvents.Any(x => x.EventClassName == eventClassName);
        found.ShouldBeTrue($"{fixture.CurrentPhase}expected an event of type {eventClassName} to have been persisted.");
        return this;
    }

    public Then WeShouldHaveTheCorrectNumberOfDomainEventsPersisted(int expectedNumber)
    {
        var actualNumber = fixture.EventRepositoryInMemory.PersistedEvents.Count;
        if (actualNumber != expectedNumber)
        {
            var eventNames = fixture.EventRepositoryInMemory.PersistedEvents.Select(x => x.EventClassName);
            var eventList = string.Join("\n  - ", eventNames);
            actualNumber.ShouldBe(
                expectedNumber,
                $"{fixture.CurrentPhase}expected {expectedNumber} domain events, but found {actualNumber}.\nEvents persisted:\n  - {eventList}");
        }
        else
        {
            actualNumber.ShouldBe(expectedNumber);
        }

        return this;
    }

    public Then WeGetTheStreamIdFromTheInitialDomainEvent(Guid requestId, out Guid streamId)
    {
        // Adapt {InitiatedEventType} to the first domain event created by the main API action.
        var persistedEvent = fixture.EventRepositoryInMemory.PersistedEvents.FirstOrDefault(
            x => x.EventClassName == typeof({InitiatedEventType}).FullName);

        persistedEvent.ShouldNotBeNull(
            $"{fixture.CurrentPhase}expected an event of type {typeof({InitiatedEventType}).FullName} to have been persisted.");

        var initiatedEvent = persistedEvent!.To<{InitiatedEventType}>();
        initiatedEvent.ShouldNotBeNull($"{fixture.CurrentPhase}expected to deserialise {typeof({InitiatedEventType}).Name}.");
        initiatedEvent!.IdempotencyKey.ShouldBe(
            requestId.ToString(),
            $"{fixture.CurrentPhase}expected the initiating event to carry request id {requestId}.");

        streamId = persistedEvent.StreamId;
        return this;
    }

    public Then AnOutboxRecordWasInserted<T>()
    {
        var typeName = (typeof(T).FullName ?? typeof(T).Name).Split('.').Last();
        fixture.OutboxRepositoryInMemory.OutboxItems.Count.ShouldBe(
            1,
            $"{fixture.CurrentPhase}expected one outbox record to have been inserted.");

        var outboxItem = fixture.OutboxRepositoryInMemory.OutboxItems.First().Value;
        outboxItem.OutboxItem.TypeName.ShouldBe(
            typeName,
            $"{fixture.CurrentPhase}expected the outbox item type name to be {typeName}.");

        return this;
    }

    public Then AMessageWasSent<T>(Func<T, bool> match, int times = 1, int retryCount = 10, int delayMs = 200)
    {
        var senderMock = fixture.FakeServiceBus.GetSenderFor<T>()
            ?? throw new InvalidOperationException($"No Mock<ServiceBusSender> found for {typeof(T).Name}.");

        RetryAction(() =>
        {
            senderMock.Verify(
                x => x.SendMessageAsync(
                    It.Is<ServiceBusMessage>(m => match(m.Body.ToObjectFromJson<T>()!)),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(times),
                $"{fixture.CurrentPhase}expected message {typeof(T).Name} to have been sent {times} time(s).");
        }, retryCount, delayMs);

        return this;
    }

    public Then AMessageWasSent<T>(int times = 1, int retryCount = 10, int delayMs = 200)
    {
        var senderMock = fixture.FakeServiceBus.GetSenderFor<T>()
            ?? throw new InvalidOperationException($"No Mock<ServiceBusSender> found for {typeof(T).Name}.");

        RetryAction(() =>
        {
            senderMock.Verify(
                x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()),
                Times.Exactly(times),
                $"{fixture.CurrentPhase}expected message {typeof(T).Name} to have been sent {times} time(s).");
        }, retryCount, delayMs);

        return this;
    }

    public Then AfterSomeTimeHasPassed(int ms = 2_500)
    {
        fixture.FakeTimeProvider.Advance(TimeSpan.FromMilliseconds(ms));
        return this;
    }

    public Then TheMessageWasHandled<T>(int times = 1, int retryCount = 25, int delayMs = 250) where T : class
    {
        var processor = fixture.FakeServiceBus.GetProcessorFor<T>()
            ?? throw new InvalidOperationException($"No TestableServiceBusProcessor found for {typeof(T).Name}.");

        RetryAction(() =>
        {
            var deliveryCount = processor.MessageDeliveryAttempts.Count;
            deliveryCount.ShouldBe(
                times,
                $"{fixture.CurrentPhase}expected ServiceBusProcessor<{typeof(T).Name}> to have {times} delivery attempt(s), found {deliveryCount}.");

            processor.MessageDeliveryAttempts[0].WasCompleted.ShouldBeTrue(
                $"{fixture.CurrentPhase}expected the event {typeof(T).Name} to have been handled.");
        }, retryCount, delayMs);

        return this;
    }

    private void RetryAction(Action action, int retryCount, int delayMs)
    {
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetry(
                retryCount: retryCount,
                sleepDurationProvider: _ => TimeSpan.FromMilliseconds(delayMs),
                onRetry: (exception, timeSpan, attempt, _) =>
                {
                    Console.WriteLine(
                        $"RetryAction attempt {attempt}/{retryCount} after {timeSpan.TotalMilliseconds}ms. {exception.Message}");
                });

        retryPolicy.Execute(action);
    }
}
```

Adaptation notes:

- Replace `{InitiatedEventType}` with the event that carries the idempotency key or request ID.
- Add more endpoint-verification helpers for each outbound HTTP service the app calls.
- If the outbox uses multiple records or different type-name conventions, adapt `AnOutboxRecordWasInserted<T>()` accordingly.
