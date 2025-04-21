using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using Azure.Messaging.ServiceBus;
using Moq;
using Moq.Contrib.HttpClient;
using WeatherApp.Application.Models.Requests;
using WeatherApp.Domain.EventSourcing;
using WeatherApp.Domain.ValueObjects;

namespace WeatherApp.Tests.Framework;

public class Given(ComponentTestFixture fixture)
{
    public Given And => this;

    public Given WeHaveAWeatherReportRequest(string region, DateTime date, out HttpRequestMessage request)
    {
        request = new HttpRequestMessage(HttpMethod.Get, $"v1/weather-forecast/{region}/{date:s}");
        return this;
    }

    public Given WeHaveSomeCollectedWeatherData(out CollectedWeatherDataModel data)
    {
        data = new CollectedWeatherDataModel([
            CannedData.GetRandomCollectedWeatherDataModel(),
            CannedData.GetRandomCollectedWeatherDataModel(),
            CannedData.GetRandomCollectedWeatherDataModel()
        ]);

        return this;
    }

    public Given WeHaveResetEverything()
    {        
        fixture.FakeServiceBus.ClearDeliveryAttemptsOnAllProcessors();
        fixture.FakeServiceBus.ClearInvocationsOnAllSenders();
        fixture.MockContributorPaymentsServiceHttpMessageHandler.Reset();
        fixture.ApiFactory.MockWeatherModelingServiceHttpMessageHandler.Reset();
        fixture.EventRepositoryInMemory.PersistedEvents.Clear();
        fixture.OutboxRepositoryInMemory.OutboxItems.Clear();

        return this;
    }

    public Given ThereIsExistingData(List<Event> existingData)
    {
        fixture.EventRepositoryInMemory.InsertExistingEvents(existingData, fixture.FakeTimeProvider);
        return this;
    }

    public Given TheServersAreStarted()
    {
        fixture.ApiFactory.Start();
        fixture.NotificationServiceFactory.Start();
        fixture.EventListenerFactory.Start();
        fixture.OutboxApplicationFactory.Start();
        
        return this;
    }

    public Given TheContributorPaymentsServicePendingEndpointWillReturn(HttpStatusCode statusCode)
    {        
        fixture.MockContributorPaymentsServiceHttpMessageHandler
            .SetupRequest(HttpMethod.Post, 
                r => r.RequestUri!.ToString().Contains("pending"))
            .Returns(async (HttpRequestMessage request, CancellationToken _) => 
            {                
                var contributorId = GetSegmentFromPath(2, request);
                var requestBody = await request.Content!.ReadFromJsonAsync<PendingContributorPayment>();
                if (requestBody == null)
                    throw new Exception("Request body was null");
                var content = new StringContent(JsonSerializer.Serialize(new { ContributorId = Guid.Parse(contributorId), requestBody.PaymentId, Status = "Pending" }));
                return new HttpResponseMessage(statusCode) { Content = content };
            });
        return this;
    }

    private string GetSegmentFromPath(int position, HttpRequestMessage request)
    {
        var splitPath = request.RequestUri!.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (splitPath.Length <= position)
            throw new Exception($"Path segment {position} was not found in the request path: {request.RequestUri.AbsolutePath}");
        
        return splitPath[position];
    }

    public Given TheContributorPaymentsServiceRevokeEndpointWillReturn(HttpStatusCode statusCode)
    {        
        fixture.MockContributorPaymentsServiceHttpMessageHandler
            .SetupRequest(HttpMethod.Post, 
                r => r.RequestUri!.ToString().StartsWith($"{Constants.BaseUrl}{Constants.ContributorPaymentsServiceUriStart}") 
                && r.RequestUri!.ToString().Contains("revoke"))
            .Returns((HttpRequestMessage request, CancellationToken ct) => 
            {                
                var contributorId = GetSegmentFromPath(2, request);
                var paymentId = GetSegmentFromPath(4, request);
                var content = new StringContent(JsonSerializer.Serialize(new { ContributorId = Guid.Parse(contributorId), PaymentId = paymentId, Status = "Revoked" }));
                return new HttpResponseMessage(statusCode) { Content = content };
            });
        return this;
    }

    public Given TheContributorPaymentsServiceCommitEndpointWillReturn(HttpStatusCode statusCode)
    {        
        fixture.MockContributorPaymentsServiceHttpMessageHandler
            .SetupRequest(HttpMethod.Post, 
                r => r.RequestUri!.ToString().StartsWith($"{Constants.BaseUrl}{Constants.ContributorPaymentsServiceUriStart}") 
                && r.RequestUri!.ToString().Contains("commit"))
           .Returns((HttpRequestMessage request, CancellationToken ct) => 
            {                
                var contributorId = GetSegmentFromPath(2, request);
                var paymentId = GetSegmentFromPath(4, request);
                var content = new StringContent(JsonSerializer.Serialize(new { ContributorId = Guid.Parse(contributorId), PaymentId = paymentId, Status = "Committed" }));
                return new HttpResponseMessage(statusCode) { Content = content };
            });
        return this;
    }

    public Given TheModelingServiceSubmitEndpointWillReturn(HttpStatusCode statusCode)
    {
        var submissionId = Guid.NewGuid();
        fixture.ApiFactory.MockWeatherModelingServiceHttpMessageHandler
            .SetupRequest(HttpMethod.Post, r => r.RequestUri!.ToString().StartsWith($"{Constants.BaseUrl}{Constants.WeatherModelingServiceSubmissionUri}"))
            .ReturnsResponse(statusCode, new StringContent(submissionId.ToString()));
        return this;
    }

    public Given MessagesSentWillBeReceived<TMessageType>() where TMessageType : class
    {
        // If there is no TestableServiceBusProcessor for the given TMessageType then just return.
        if (fixture.FakeServiceBus.HasProcessorFor<TMessageType>() == false)
            return this;

        var senderMock = fixture.FakeServiceBus.GetSenderFor<TMessageType>();
        // If there is no senderMock for the given TMessageType then just return.
        if (senderMock == null)
            return this;

        senderMock.Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((sbm, ctx) =>
            {
                var message = sbm.Body.ToObjectFromJson<TMessageType>();
                var applicationProperties = (Dictionary<string, object>?)sbm.ApplicationProperties;

                var processor = fixture.FakeServiceBus.GetProcessorFor<TMessageType>();

                if(message == null)
                    throw new Exception($"Message of type {typeof(TMessageType).Name} was null");

                processor.SendMessage(message, applicationProperties: applicationProperties).GetAwaiter().GetResult();
            });

        return this;
    }
}