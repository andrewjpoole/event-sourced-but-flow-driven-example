using System.Net;
using Moq.Contrib.HttpClient;
using WeatherApp.Application.Models.Requests;
using WeatherApp.Infrastructure.ApiClients.NotificationService;

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
            CannedData.GetRandCollectedWeatherDataModel(),
            CannedData.GetRandCollectedWeatherDataModel(),
            CannedData.GetRandCollectedWeatherDataModel()
        ]);

        return this;
    }

    public Given TheServersAreStarted()
    {
        fixture.ApiFactory.Start();
        fixture.NotificationServiceFactory.Start();
        fixture.EventListenerFactory.Start();

        // Replace the httpClient in eventlistener's IoC container with the in-memory one from the NotificationServiceFactory.
        fixture.EventListenerFactory.ClearHttpClients();
        fixture.EventListenerFactory.AddHttpClient(typeof(INotificationsClient).FullName!, fixture.NotificationServiceFactory.HttpClient!); 
        return this;
    }

    public Given TheModelingServiceSubmitEndpointWillReturn(HttpStatusCode statusCode)
    {
        var submissionId = Guid.NewGuid();
        fixture.ApiFactory.MockWeatherModelingServiceHttpMessageHandler
            .SetupRequest(HttpMethod.Post, r => r.RequestUri!.ToString().StartsWith($"{Constants.WeatherModelingServiceBaseUrl}{Constants.WeatherModelingServiceSubmissionUri}"))
            .ReturnsResponse(statusCode, new StringContent(submissionId.ToString()));
        return this;
    }
}