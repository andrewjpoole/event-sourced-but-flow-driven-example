using System.Text;
using System.Text.Json;

namespace WeatherApp.Tests.Aspire.Integration.Framework;

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
