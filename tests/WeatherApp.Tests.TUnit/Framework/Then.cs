using System.Net;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Moq;
using Moq.Contrib.HttpClient;
using WeatherApp.Domain.EventSourcing;
using WeatherApp.Domain.DomainEvents;
using Shouldly;
using Polly;
using Polly.Retry;

namespace WeatherApp.Tests.TUnit.Framework;

public class Then(ComponentTestFixture fixture)
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
        response.StatusCode.ShouldBe(code, $"In {fixture.CurrentPhase}, expected that the response code was {code}.");        
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
        TheBodyShouldNotBeEmpty(response, out var body);
        body.ShouldNotBeEmpty($"{fixture.CurrentPhase}expected that the response body would not be empty.");
        return this;
    }

    public Then TheBodyShouldNotBeEmpty(HttpResponseMessage response, Action<string>? assertAgainstBody = null)
    {
        TheBodyShouldNotBeEmpty(response, out var body);        
        body.ShouldNotBeEmpty($"{fixture.CurrentPhase}expected that the response body would not be empty.");
        assertAgainstBody?.Invoke(body);
        return this;
    }    

    public Then TheBodyShouldNotBeEmpty<T>(HttpResponseMessage response, out T bodyAsT)
    {
        TheBodyShouldNotBeEmpty(response, out var body);
        T bodyT;
        try
        {
            bodyT = JsonSerializer.Deserialize<T>(body, GlobalJsonSerialiserSettings.Default) ?? throw new Exception();
        }
        catch (Exception e)
        {
            var typeOfT = typeof(T);
            throw new Exception($"{fixture.CurrentPhase}Then.TheBodyShouldNotBeEmpty<{typeOfT.Name}>(). Unable to deserialise response body into {typeOfT.Name}. Body:{body}", e);
        }

        bodyAsT = bodyT;

        return this;
    }

    public Then TheBodyShouldNotBeEmpty<T>(HttpResponseMessage response, Action<T>? assertAgainstBody = null)
    {
        TheBodyShouldNotBeEmpty<T>(response, out var bodyT);
        
        assertAgainstBody?.Invoke(bodyT);

        return this;
    }
    
    public Then TheModelingServiceSubmitEndpointShouldHaveBeenCalled(int times = 1)
    {
        fixture.MockWeatherModelingServiceHttpMessageHandler
            .VerifyRequest(HttpMethod.Post, 
                r => r.RequestUri!.ToString().StartsWith($"{Constants.BaseUrl}{Constants.WeatherModelingServiceSubmissionUri}"), 
                Times.Exactly(times), $"{fixture.CurrentPhase}expected the ModelingServiceSubmitEndpoint to have been called {times} time(s).");

        return this;
    }

    public Then TheModelingServiceSubmitEndpointShouldNotHaveBeenCalled() => 
        TheModelingServiceSubmitEndpointShouldHaveBeenCalled(0);

    public Then TheContributorPaymentsServiceCreateEndpointShouldHaveBeenCalled(int times = 1)
    {
        fixture.MockContributorPaymentsServiceHttpMessageHandler
            .VerifyRequest(HttpMethod.Post, 
                r => r.RequestUri!.ToString().StartsWith($"{Constants.BaseUrl}{Constants.ContributorPaymentsServiceUriStart}") 
                && r.RequestUri!.ToString().EndsWith("/pending"), 
                Times.Exactly(times), $"{fixture.CurrentPhase}expected the ContributorPaymentsServiceCreateEndpoint to have been called {times} time(s).");

        return this;
    }

    public Then TheContributorPaymentsServiceCreateEndpointShouldNotHaveBeenCalled() => 
        TheContributorPaymentsServiceCreateEndpointShouldHaveBeenCalled(0);

    public Then TheContributorPaymentsServiceCommitEndpointShouldHaveBeenCalled(int times = 1)
    {
        fixture.MockContributorPaymentsServiceHttpMessageHandler
            .VerifyRequest(HttpMethod.Post, 
                r => r.RequestUri!.ToString().StartsWith($"{Constants.BaseUrl}{Constants.ContributorPaymentsServiceUriStart}") 
                && r.RequestUri!.ToString().Contains("/commit/"), 
                Times.Exactly(times), $"{fixture.CurrentPhase}expected the ContributorPaymentsServiceCommitEndpoint to have been called {times} time(s).");

        return this;
    }

    public Then TheContributorPaymentsServiceCommitEndpointShouldNotHaveBeenCalled() => 
        TheContributorPaymentsServiceCommitEndpointShouldHaveBeenCalled(0);

    public Then TheDomainEventShouldHaveBeenPersisted<T>()
    {
        var typeOfT = typeof(T);
        var eventClassName = typeOfT.FullName ?? typeOfT.Name;

        var found = fixture.EventRepositoryInMemory?.PersistedEvents.Any(e => e.EventClassName == eventClassName) ?? false;
        found.ShouldBeTrue($"\n{fixture.CurrentPhase}expected an event of type {eventClassName} to have been persisted in the database.");

        return this;
    }

    public Then WeShouldHaveTheCorrectNumberOfDomainEventsPersisted(int expectedNumber)
    {
        var actualNumber = fixture.EventRepositoryInMemory?.PersistedEvents.Count ?? 0;
        
        if (actualNumber != expectedNumber)
        {
            var eventNames = fixture.EventRepositoryInMemory?.PersistedEvents.Select(e => e.EventClassName).ToList() ?? new List<string>();
            var eventList = string.Join("\n  - ", eventNames);
            actualNumber.ShouldBe(expectedNumber, $"{fixture.CurrentPhase}expected {expectedNumber} domain events to have been persisted, but found {actualNumber}.\nEvents persisted:\n  - {eventList}");
        }
        else
        {
            actualNumber.ShouldBe(expectedNumber);
        }

        return this;
    }

    public Then WeGetTheStreamIdFromTheInitialDomainEvent(Guid requestId, out Guid streamId)
    {
        var initialisedPersistedEvent = fixture.EventRepositoryInMemory?.PersistedEvents.FirstOrDefault(
            e => e.EventClassName == typeof(WeatherDataCollectionInitiated).FullName);
        initialisedPersistedEvent.ShouldNotBeNull($"{fixture.CurrentPhase}expected an event of type {typeof(WeatherDataCollectionInitiated).FullName} to have been persisted in the database.");

        var initialisedEvent = initialisedPersistedEvent?.To<WeatherDataCollectionInitiated>();
        initialisedEvent.ShouldNotBeNull($"{fixture.CurrentPhase}expected an event of type {typeof(WeatherDataCollectionInitiated).FullName} to have been persisted in the database.");

        initialisedEvent!.IdempotencyKey.ShouldBe(requestId.ToString(), $"{fixture.CurrentPhase}expected the event to have a request id of {requestId}.");

        streamId = initialisedPersistedEvent!.StreamId;

        return this;
    }

    public Then AnOutboxRecordWasInserted<T>()
    {
        var typeOfT = typeof(T);
        var outboxClassName = (typeOfT.FullName ?? typeOfT.Name).Split('.').Last();

        fixture.OutboxRepositoryInMemory.OutboxItems.Count.ShouldBe(1, $"{fixture.CurrentPhase}expected one outbox record to have been inserted.");
        
        var outboxItemWithSentStatuses = fixture.OutboxRepositoryInMemory.OutboxItems.First().Value;        
        
        outboxItemWithSentStatuses.OutboxItem.TypeName.ShouldBe(outboxClassName, $"{fixture.CurrentPhase}expected the outbox item to have a MessagingEntityName of {outboxClassName}.");

        return this;
    }

    

    public Then AMessageWasSent<T>(Func<T, bool> match, int times = 1, int numberOfRetries = 10, int retryDelayInMilliSeconds = 200)
    {
        var senderMock = fixture.FakeServiceBus.GetSenderFor<T>() ?? 
            throw new Exception($"No Mock<ServiceBusSender> found for message type {typeof(T).Name}");

        // var retryPolicy = Policy
        //     .Handle<Exception>()
        //     .WaitAndRetry(
        //         retryCount: numberOfRetries,
        //         sleepDurationProvider: _ => TimeSpan.FromMilliseconds(retryDelayInMilliSeconds),
        //         onRetry: (exception, timeSpan, retryCount, context) =>
        //         {
        //             Console.WriteLine($"{fixture.CurrentPhase}AMessageWasSent<{typeof(T).Name}> retry attempt {retryCount}/{numberOfRetries} after {timeSpan.TotalMilliseconds}ms delay. Exception: {exception.Message}");
        //         });

        // retryPolicy.Execute(() =>
        // {
        //     senderMock.Verify(x => x.SendMessageAsync(
        //         It.Is<ServiceBusMessage>(m => match(m.Body.ToObjectFromJson<T>()!)), 
        //         It.IsAny<CancellationToken>()), Times.Exactly(times), 
        //         $"{fixture.CurrentPhase}expected message of type {typeof(T).Name} to have been sent {times} time(s).");
        // });

        RetryAction(() =>
        {
            senderMock.Verify(x => x.SendMessageAsync(
                It.Is<ServiceBusMessage>(m => match(m.Body.ToObjectFromJson<T>()!)), 
                It.IsAny<CancellationToken>()), Times.Exactly(times), 
                $"{fixture.CurrentPhase}expected message of type {typeof(T).Name} to have been sent {times} time(s).");
        }, numberOfRetries, retryDelayInMilliSeconds);

        return this;
    }

    public Then AMessageWasSent<T>(int times = 1, int numberOfRetries = 10, int retryDelayInMilliSeconds = 200)
    {
        var senderMock = fixture.FakeServiceBus.GetSenderFor<T>() ?? 
            throw new Exception($"No Mock<ServiceBusSender> found for message type {typeof(T).Name}");

        // var retryPolicy = Policy
        //     .Handle<Exception>()
        //     .WaitAndRetry(
        //         retryCount: numberOfRetries,
        //         sleepDurationProvider: _ => TimeSpan.FromMilliseconds(retryDelayInMilliSeconds),
        //         onRetry: (exception, timeSpan, retryCount, context) =>
        //         {
        //             Console.WriteLine($"{fixture.CurrentPhase}AMessageWasSent<{typeof(T).Name}> retry attempt {retryCount}/{numberOfRetries} after {timeSpan.TotalMilliseconds}ms delay. Exception: {exception.Message}");
        //         });

        // retryPolicy.Execute(() =>
        // {
        //     senderMock.Verify(x => x.SendMessageAsync(
        //         It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(times), 
        //         $"{fixture.CurrentPhase}expected message of type {typeof(T).Name} to have been sent {times} time(s).");
        // });

        RetryAction(() =>
        {
            senderMock.Verify(x => x.SendMessageAsync(
                It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(times), 
                $"{fixture.CurrentPhase}expected message of type {typeof(T).Name} to have been sent {times} time(s).");
        }, numberOfRetries, retryDelayInMilliSeconds);

        return this;
    }


    public Then AfterSomeTimeHasPassed(int numberOfMsToAdvance = 2_500)
    {
        // Advance the time so the outbox processor wakes up to check for messages...
        fixture.FakeTimeProvider.Advance(TimeSpan.FromMilliseconds(numberOfMsToAdvance));
        // So cool!😁

        return this;
    }

    public Then TheMessageWasHandled<T>(int times = 1, int numberOfReties = 25, int retryDelayInMilliSeconds = 250) where T : class
    {
        // var retryPolicy = Policy
        //     .Handle<Exception>()
        //     .WaitAndRetry(
        //         retryCount: numberOfReties,
        //         sleepDurationProvider: _ => TimeSpan.FromMilliseconds(retryDelayInMilliSeconds),
        //         onRetry: (exception, timeSpan, retryCount, context) =>
        //         {
        //             Console.WriteLine($"TheMessageWasHandled<{typeof(T).Name}> retry attempt {retryCount}/{numberOfReties} after {timeSpan.TotalMilliseconds}ms delay. Exception: {exception.Message}");
        //         });

        // retryPolicy.Execute(() =>
        // {
        //     var processor = fixture.FakeServiceBus.GetProcessorFor<T>();
        //     var deliveryCount = processor.MessageDeliveryAttempts.Count;
            
        //     deliveryCount.ShouldBe(1, $"{fixture.CurrentPhase}in TheMessageWasHandled<{typeof(T).Name}>, expected the ServiceBusProcesser<{typeof(T).Name}> to have had a single delivery attempt, instead found {deliveryCount}.");
        //     processor.MessageDeliveryAttempts[0].WasCompleted.ShouldBeTrue($"{fixture.CurrentPhase}expected the event {typeof(T).Name} to have been handled.");
        // });

        var processor = fixture.FakeServiceBus.GetProcessorFor<T>() ?? 
            throw new Exception($"No TestableServiceBusProcessor found for message type {typeof(T).Name}");

        RetryAction(() =>
        {            
            var deliveryCount = processor.MessageDeliveryAttempts.Count;
            
            deliveryCount.ShouldBe(times, $"{fixture.CurrentPhase}in TheMessageWasHandled<{typeof(T).Name}>, expected the ServiceBusProcesser<{typeof(T).Name}> to have had a {times} attempt(s), instead found {deliveryCount}.");
            processor.MessageDeliveryAttempts[0].WasCompleted.ShouldBeTrue($"{fixture.CurrentPhase}expected the event {typeof(T).Name} to have been handled.");
        }, numberOfReties, retryDelayInMilliSeconds);

        return this;
    }

    private void RetryAction(Action action, int numberOfReties, int retryDelayInMilliSeconds)
    {
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetry(
                retryCount: numberOfReties,
                sleepDurationProvider: _ => TimeSpan.FromMilliseconds(retryDelayInMilliSeconds),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    Console.WriteLine($"RetryAction retry attempt {retryCount}/{numberOfReties} after {timeSpan.TotalMilliseconds}ms delay. Exception: {exception.Message}");
                });

        retryPolicy.Execute(() =>
        {
            action();
        });
    }
}