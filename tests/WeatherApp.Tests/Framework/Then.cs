using System.Net;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Moq;
using Moq.Contrib.HttpClient;
using WeatherApp.Domain.EventSourcing;

namespace WeatherApp.Tests.Framework;

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
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(code, $"In {fixture.CurrentPhase}, expected that the response code was {code}.");
        return this;
    }

    public Then TheBodyShouldBeEmpty(HttpResponseMessage response)
    {
        var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        body.Should().BeEmpty($"{fixture.CurrentPhase}expected that the response body would be empty.");

        return this;
    }

    public Then TheBodyShouldNotBeEmpty(HttpResponseMessage response, out string body)
    {
        body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        body.Should().NotBeEmpty($"{fixture.CurrentPhase}expected that the response body would not be empty.");

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

    public Then TheEventShouldHaveBeenPersisted<T>()
    {
        var typeOfT = typeof(T);
        var eventClassName = typeOfT.FullName ?? typeOfT.Name;
        fixture.EventRepositoryInMemory?.PersistedEvents.Should().Contain(e => e.EventClassName == eventClassName,
            $"\n{fixture.CurrentPhase}expected an event of type {eventClassName} to have been persisted in the database.");

        return this;
    }

    public Then AnOutboxRecordWasInserted()
    {
        

        return this;
    }

    public Then AMessageWasSent(Mock<ServiceBusSender> senderMock, int times = 1)
    {
        senderMock.Verify(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(times));

        return this;
    }

    public Then AMessageWasSent(Mock<ServiceBusSender> senderMock, Func<ServiceBusMessage, bool> match, int times = 1)
    {
        senderMock.Verify(x => x.SendMessageAsync(It.Is<ServiceBusMessage>(m => match(m)), It.IsAny<CancellationToken>()), Times.Exactly(times));

        return this;
    }

    public Then AfterSomeTimeHasPassed(int numberOfMsToAdvance = 2000, int numberOfMsToWait = 2000)
    {
        // Advance the time so the outbox processor wakes up to check for messages...
        fixture.FakeTimeProvider.Advance(TimeSpan.FromMilliseconds(numberOfMsToAdvance)); // So cool!😁

        // Wait for the outbox processor to process the messages. This is not ideal, but it works for now.
        Task.Delay(numberOfMsToWait).GetAwaiter().GetResult();

        return this;
    }

    public Then TheMessageWasHandled<TIntegrationEvent>() where TIntegrationEvent : class
    {
        var processor = fixture.MockServiceBus.GetProcessorFor<TIntegrationEvent>();
        processor.MessageDeliveryAttempts.Count.Should().Be(1, $"{fixture.CurrentPhase}expected the NotificationService to have handled the event {typeof(TIntegrationEvent).Name}.");

        processor.MessageDeliveryAttempts[0].WasCompleted.Should().BeTrue($"{fixture.CurrentPhase}expected the NotificationService to have handled the event {typeof(TIntegrationEvent).Name}.");

        return this;
    }

    public Then TheNotificationServiceNotifiedTheUser(string location, string reference)
    {
        var realSentNotifications = fixture.NotificationServiceFactory.RealSentNotifications;

        realSentNotifications.Should().NotBeNull($"{fixture.CurrentPhase}expected the NotificationService to have instantiated it's SentNoticiations list.");
        realSentNotifications.Count.Should().Be(1, $"{fixture.CurrentPhase}expected SentNotifications list to contain a single item.");
        realSentNotifications[0].Reference.Should().Be(reference, $"{fixture.CurrentPhase}expected the SentNotification to have the reference {reference}.");

        var expectedBody = "Dear user, your data has been submitted and included in our latest model";
        realSentNotifications[0].Body.Should().Be(expectedBody, $"{fixture.CurrentPhase}expected the SentNotification to have expected body.");

        return this;
    }
}