using System.Net;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Moq;
using Moq.Contrib.HttpClient;
using WeatherApp.Domain.EventSourcing;
using WeatherApp.Domain.DomainEvents;
using Shouldly;

namespace WeatherApp.Tests.TUnit.Framework;

public class Then(ComponentTestFixture fixture)
{
    public Then And => this;

    public Then AndAssert(Action assertion)
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
        fixture.ApiFactory.MockWeatherModelingServiceHttpMessageHandler
            .VerifyRequest(HttpMethod.Post, 
                r => r.RequestUri!.ToString().StartsWith($"{Constants.BaseUrl}{Constants.WeatherModelingServiceSubmissionUri}"), 
                Times.Exactly(times), $"{fixture.CurrentPhase}expected the ModelingServiceSubmitEndpoint to have been called {times} time(s).");

        return this;
    }

    public Then TheModelingServiceSubmitEndpointShouldNotHaveBeenCalled() => 
        TheModelingServiceSubmitEndpointShouldHaveBeenCalled(0);

    public Then TheDomainEventShouldHaveBeenPersisted<T>()
    {
        var typeOfT = typeof(T);
        var eventClassName = typeOfT.FullName ?? typeOfT.Name;

        var found = fixture.EventRepositoryInMemory?.PersistedEvents.Any(e => e.EventClassName == eventClassName) ?? false;
        found.ShouldBeTrue($"\n{fixture.CurrentPhase}expected an event of type {eventClassName} to have been persisted in the database.");

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

    public Then AMessageWasSent(Mock<ServiceBusSender> senderMock, int times = 1)
    {
        senderMock.Verify(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(times));

        return this;
    }

    public Then AMessageWasSent(Mock<ServiceBusSender> senderMock, Func<ServiceBusMessage, bool> match, int times = 1)
    {
        senderMock.Verify(x => x.SendMessageAsync(
            It.Is<ServiceBusMessage>(m => match(m)), 
            It.IsAny<CancellationToken>()), Times.Exactly(times));

        return this;
    }

    public Then AfterSomeTimeHasPassed(int numberOfMsToAdvance = 2000, int numberOfMsToWait = 2000)
    {
        // Advance the time so the outbox processor wakes up to check for messages...
        fixture.FakeTimeProvider.Advance(TimeSpan.FromMilliseconds(numberOfMsToAdvance));
        // So cool!😁

        // Wait for the outbox processor to process the messages. This is not ideal, but it works for now.
        Task.Delay(numberOfMsToWait).GetAwaiter().GetResult();

        return this;
    }

    public Then TheMessageWasHandled<TIntegrationEvent>() where TIntegrationEvent : class
    {
        var processor = fixture.FakeServiceBus.GetProcessorFor<TIntegrationEvent>();
        var deliveryCount = processor.MessageDeliveryAttempts.Count;
        {
            deliveryCount.ShouldBe(1, $"{fixture.CurrentPhase}expected the ServiceBusProcesser<{typeof(TIntegrationEvent).Name}> to have had a single delivery attempt, instead found {deliveryCount}.");
            processor.MessageDeliveryAttempts[0].WasCompleted.ShouldBeTrue($"{fixture.CurrentPhase}expected the NotificationService to have handled the event {typeof(TIntegrationEvent).Name}.");
        }

        return this;
    }

    public Then TheNotificationServiceNotifiedTheUser(string location, string reference)
    {
        var realSentNotifications = fixture.NotificationServiceFactory.RealSentNotifications;

        realSentNotifications.ShouldNotBeNull($"{fixture.CurrentPhase}expected the NotificationService to have instantiated its SentNotifications list.");
        {
            realSentNotifications!.Count.ShouldBe(1, $"{fixture.CurrentPhase}expected SentNotifications list to contain a single item.");
            realSentNotifications[0].Reference.ShouldBe(reference, $"{fixture.CurrentPhase}expected the SentNotification to have the reference {reference}.");

            var expectedBody = "Dear user, your data has been submitted and included in our latest model";
            realSentNotifications[0].Body.ShouldBe(expectedBody, $"{fixture.CurrentPhase}expected the SentNotification to have the expected body.");
        }

        return this;
    }
}