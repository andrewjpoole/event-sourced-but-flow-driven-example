using System.Net;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FluentAssertions;
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
                r => r.RequestUri!.ToString().StartsWith($"{Constants.WeatherModelingServiceBaseUrl}{Constants.WeatherModelingServiceSubmissionUri}"), 
                Times.Exactly(times), $"{fixture.CurrentPhase}expected the ModelingServiceSubmitEndpoint to have been called {times} time(s).");

        return this;
    }

    public Then TheEventShouldHaveBeenPersisted<T>()
    {
        var typeOfT = typeof(T);
        var eventClassName = typeOfT.FullName ?? typeOfT.Name;
        fixture.EventRepositoryInMemory?.PersistedEvents.Should().Contain(e => e.EventClassName == eventClassName,
            $"{fixture.CurrentPhase}expected an event of type {eventClassName} to have been persisted in the database.");

        return this;
    }

    public Then ANotificationShouldHaveBeenSent(string testLocation)
    {
        fixture.NotificationServiceFactory.NotificationHandler?.Notifications.Count.Should().Be(1);
        fixture.NotificationServiceFactory.NotificationHandler?.Notifications.First().Value.Body.Should().Contain(testLocation);

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

    public Then AMessageWasSent<TMessageType>(Func<ServiceBusMessage, bool> match, int times = 1, bool tryForwardToProcessor = true) where TMessageType : class
    {
        var senderMock = fixture.MockServiceBusSenders.GetSenderFor<TMessageType>();
        senderMock.Verify(x => x.SendMessageAsync(It.Is<ServiceBusMessage>(m => match(m)), It.IsAny<CancellationToken>()), Times.Exactly(times));

        senderMock.Setup(x => x.SendMessageAsync(It.Is<ServiceBusMessage>(m => match(m)), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((sbm, ctx) =>
            {
                if (fixture.EventListenerFactory.TestableServiceBusProcessors.HasProcessorFor<TMessageType>() == false)
                    return;

                var message = sbm.Body.ToObjectFromJson<TMessageType>();
                var applicationProperties = (Dictionary<string, object>?)sbm.ApplicationProperties;
                
                var processor = fixture.EventListenerFactory.TestableServiceBusProcessors.GetProcessorFor<TMessageType>();
                processor.SendMessage(message, applicationProperties: applicationProperties).GetAwaiter().GetResult();
            });

        return this;
    }
}



