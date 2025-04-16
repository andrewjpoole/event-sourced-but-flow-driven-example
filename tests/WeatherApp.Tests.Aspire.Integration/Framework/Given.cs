using System.Text;
using System.Text.Json;
using Aspire.Hosting;
using Microsoft.Extensions.Logging;

namespace WeatherApp.Tests.Aspire.Integration.Framework;

public class Given
{
    public Given And => this;

    public Given WeHaveSetupTheAppHost(out IDistributedApplicationTestingBuilder appHost)
    {
        appHost = DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>()
            .GetAwaiter().GetResult();

        appHost.Resources.FirstOrDefault(x => x.Name == "outbox")?
            .Annotations.Add(new EnvironmentCallbackAnnotation("OutboxProcessorOptions__IntervalBetweenBatchesInSeconds", () => "2"));

        // var subscription = appHost.Eventing.Subscribe<ResourceReadyEvent>((@event, ct) => 
        // {
        //     return Task.CompletedTask;
        // });
        
        var appName = appHost.Environment.ApplicationName;
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter(appName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
        });        

        return this;
    }    

    public Given WeRunTheAppHost(IDistributedApplicationTestingBuilder appHost, out DistributedApplication app, TimeSpan defaultTimeout)
    {
        app = appHost.BuildAsync().GetAwaiter().GetResult();
        app.StartAsync().Wait(defaultTimeout);
        return this;
    }

    public Given WeCreateAnHttpClientForTheQueryableTraceCollector(DistributedApplication app, out HttpClient client)
    {
        client = app.CreateHttpClient("queryabletracecollector");
        app.ResourceNotifications.WaitForResourceHealthyAsync("queryabletracecollector").Wait();
        return this;
    }

    public Given WeClearAnyCollectedTraces(HttpClient client)
    {
        client.DeleteAsync("/traces").Wait();
        return this;
    }

    public Given WeCreateAnHtppClientForTheAPI(DistributedApplication app, out HttpClient client, TimeSpan defaultTimeout)
    {
        client = app.CreateHttpClient("api");
        app.ResourceNotifications.WaitForResourceHealthyAsync("api").Wait(defaultTimeout);
        return this;
    }    

    public Given WeHaveSomeCollectedWeatherData(out string location, out string reference, out CollectedWeatherDataModel collectedWeatherData)
    {
        location = "TestLocation";
        reference = "TestReference";
        collectedWeatherData = new CollectedWeatherDataModel(
            new List<CollectedWeatherDataPointModel>
                {
                    new CollectedWeatherDataPointModel(
                        DateTimeOffset.UtcNow,
                        10.5m,
                        "N",
                        25.3m,
                        60.2m)
                });

        return this;
    }
}

public class When
{
    public When And => this;

    public When WeWrapTheWeatherDataInAnHttpRequest(out HttpRequestMessage request, string location, string reference, CollectedWeatherDataModel collectedWeatherData)
    {
        request = new HttpRequestMessage(HttpMethod.Post, $"v1/collected-weather-data/{location}/{reference}");
        request.Content = new StringContent(JsonSerializer.Serialize(collectedWeatherData), Encoding.UTF8, "application/json");
        return this;
    }

    public When WeSendTheRequest(HttpClient client, HttpRequestMessage request, out HttpResponseMessage response, int clientTimeout = 120)
    {
        client.Timeout = TimeSpan.FromSeconds(clientTimeout);
        response = client.SendAsync(request).Result;
        return this;
    }

    // Traces seem to be collected out of order, so we need to wait for the correct number _and_ the specific last trace to be present.
    public When WeWaitWhilePollingForTheNotificationTrace(
        HttpClient client, 
        int requiredNumberOfTraces,
        string requiredTraceName,
        out List<TraceData> traces,
        int numberOfAttempts = 60, 
        int delayBetweenAttemptsInMs = 500)
    {   
        traces = new List<TraceData>();     
        var traceCount = -1;
        var namedTraceFound = false;
        for (var i = 0; i < numberOfAttempts; i++)
        {
            var allTracesResponse = client.GetAsync("/traces").Result;
            var tracesJson = allTracesResponse.Content.ReadAsStringAsync().Result;
            traces = JsonSerializer.Deserialize<List<TraceData>>(tracesJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("Deserialization failed.");
            
            traceCount = traces.Count;
            if(traceCount >= requiredNumberOfTraces 
            && traces.Select(x => x.DisplayName).Contains(requiredTraceName))
            {
                namedTraceFound = true;
                break;
            }

            Task.Delay(delayBetweenAttemptsInMs).Wait();
        }

        if (traceCount == -1)
            Assert.Fail("Unable to retrieve any traces.");

        if (traceCount < requiredNumberOfTraces)
            Assert.Fail($"Count of traces was {traceCount}, within the timeout period {numberOfAttempts} x {delayBetweenAttemptsInMs}ms = {numberOfAttempts * delayBetweenAttemptsInMs}ms.");

        if(namedTraceFound == false)
            Assert.Fail($"Trace not found with a DisplayName of {requiredTraceName} within the timeout period {numberOfAttempts} x {delayBetweenAttemptsInMs}ms = {numberOfAttempts * delayBetweenAttemptsInMs}ms.");

        // We found at least the expected number of traces including the specific one we need.
        return this;        
    }
}

public class Then
{
    public Then And => this;

    public Then TheResponseShouldBe(HttpResponseMessage response, HttpStatusCode expectedStatusCode)
    {
        Assert.That(response.StatusCode, Is.EqualTo(expectedStatusCode));
        return this;
    }
    
    public Then TheResponseShouldBeOfType<T>(HttpResponseMessage response, out T result)
    {
        var content = response.Content.ReadAsStringAsync().Result;
        result = JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Deserialization failed.");

        return this;
    }   

    public Then WeAssertAgainstTheTraces(List<TraceData> traces, Action<List<TraceData>> assertAgainstTraces)
    {
        if (traces == null || traces.Count == 0)
            Assert.Fail("No traces found.");

        assertAgainstTraces(traces!);

        return this;
    }
}
