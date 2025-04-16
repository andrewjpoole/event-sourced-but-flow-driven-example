using System.Text.Json;

namespace WeatherApp.Tests.Aspire.Integration.Framework;

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
