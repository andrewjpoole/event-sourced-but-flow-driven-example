# When template

Purpose: hold action helpers for the component-test DSL. Adapt request-building methods, API routes, headers, and inbound integration message types for the target app.

```csharp
using System.Text;
using System.Text.Json;

namespace {Namespace}.Tests.TUnit.Framework;

public sealed class When(ComponentTestFixture fixture)
{
    public When And => this;

    public When InPhase(string phase)
    {
        fixture.SetPhase(phase);
        return this;
    }

    public When WeSendTheMessageToTheApi(HttpRequestMessage request, out HttpResponseMessage response)
    {
        if (fixture.ApiFactory.HttpClient is null)
        {
            throw new InvalidOperationException(
                "The API HttpClient has not been initialised. Call Given.TheServersAreStarted() first.");
        }

        // Synchronous wrapper keeps the fluent style close to the WeatherApp example.
        response = fixture.ApiFactory.HttpClient.SendAsync(request).GetAwaiter().GetResult();
        return this;
    }

    public When WeWrap{RequestName}InAnHttpRequestMessage(
        {RequestType} payload,
        CannedData cannedData,
        string reference,
        out HttpRequestMessage request)
    {
        request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{Constants.{ApiRouteConstant}}/{cannedData.{RouteSegment1}}/{reference}");

        // Copy the same headers the real endpoint expects.
        request.Headers.Add("x-request-id", cannedData.RequestId.ToString());
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload, GlobalJsonSerialiserSettings.Default),
            Encoding.UTF8,
            "application/json");

        return this;
    }

    public When AMessageAppears<T>(T message) where T : class
    {
        // Directly present the message to the app's registered handler.
        var processor = fixture.FakeServiceBus.GetProcessorFor<T>();
        processor.PresentMessage(message).GetAwaiter().GetResult();
        return this;
    }
}
```

Adaptation notes:

- Add one request-wrapper method per main API entry point you want to exercise.
- Reuse `InPhase(string)` in multi-phase tests so later assertions can include a useful label.
- If the app exposes multiple APIs, add methods that target the correct factory/client for each host.
