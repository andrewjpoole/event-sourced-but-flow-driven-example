# Given template

Purpose: hold readable precondition helpers for the component-test DSL. Adapt external service names, URL predicates, seeded events, and any domain-specific helper methods for the target app.

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Moq;
using Moq.Contrib.HttpClient;
using {Namespace}.Domain.EventSourcing;

namespace {Namespace}.Tests.TUnit.Framework;

public sealed class Given(ComponentTestFixture fixture)
{
    public Given And => this;

    public Given TheServersAreStarted()
    {
        // Start every hosted app that participates in the test.
        fixture.ApiFactory.Start();
        fixture.EventListenerFactory.Start();
        fixture.OutboxFactory.Start();
        return this;
    }

    public Given WeHaveResetEverything()
    {
        // Reset fakes and mocks so each scenario starts cleanly.
        fixture.FakeServiceBus.ClearDeliveryAttemptsOnAllProcessors();
        fixture.FakeServiceBus.ClearInvocationsOnAllSenders();
        fixture.Mock{ExternalService1}HttpMessageHandler.Reset();
        fixture.Mock{ExternalService2}HttpMessageHandler.Reset();
        fixture.EventRepositoryInMemory.PersistedEvents.Clear();
        fixture.OutboxRepositoryInMemory.OutboxItems.Clear();
        return this;
    }

    public Given ThereIsExistingData(List<Event> events)
    {
        fixture.EventRepositoryInMemory.InsertExistingEvents(events, fixture.FakeTimeProvider);
        return this;
    }

    public Given The{ExternalService1}EndpointWillReturn(HttpStatusCode statusCode)
    {
        fixture.Mock{ExternalService1}HttpMessageHandler
            .SetupRequest(
                HttpMethod.Post,
                request => request.RequestUri!.ToString().StartsWith($"{Constants.BaseUrl}{Constants.{ExternalService1}UriStart}"))
            .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
            {
                // Read the request body if you need to shape the fake response.
                var payloadJson = request.Content is null
                    ? string.Empty
                    : request.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                var responseBody = JsonSerializer.Serialize(new
                {
                    RequestSeen = true,
                    Service = "{ExternalService1}",
                    Payload = payloadJson
                });

                return new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(responseBody)
                };
            });

        return this;
    }

    public Given The{ExternalService2}EndpointWillReturn(HttpStatusCode statusCode)
    {
        fixture.Mock{ExternalService2}HttpMessageHandler
            .SetupRequest(
                HttpMethod.Get,
                request => request.RequestUri!.ToString().Contains(Constants.{ExternalService2}UriStart))
            .ReturnsResponse(statusCode, new StringContent("{}"));

        return this;
    }

    public Given MessagesSentWillBeReceived<TMessageType>() where TMessageType : class
    {
        // This is useful when a message published by one app should immediately feed another app.
        if (!fixture.FakeServiceBus.HasProcessorFor<TMessageType>())
        {
            return this;
        }

        var senderMock = fixture.FakeServiceBus.GetSenderFor<TMessageType>();
        if (senderMock is null)
        {
            return this;
        }

        senderMock
            .Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((serviceBusMessage, _) =>
            {
                var message = serviceBusMessage.Body.ToObjectFromJson<TMessageType>();
                if (message is null)
                {
                    throw new InvalidOperationException($"Unable to deserialise {typeof(TMessageType).Name} from Service Bus body.");
                }

                var applicationProperties = (Dictionary<string, object>?)serviceBusMessage.ApplicationProperties;
                fixture.FakeServiceBus
                    .GetProcessorFor<TMessageType>()
                    .PresentMessage(message, applicationProperties: applicationProperties)
                    .GetAwaiter()
                    .GetResult();
            });

        return this;
    }
}
```

URL matching notes:

- Prefer `StartsWith` when the app uses predictable route prefixes.
- Use `Contains` or inspect path segments when IDs are embedded in the URL.
- Match against the same base address and relative URI shape used by the production `HttpClient` registration.
