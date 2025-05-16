# Aspire Hosting QueryableTraceCollector

The QueryableTraceCollector is a lightweight API designed for collecting and querying OpenTelemetry traces. It is particularly useful for integration test assertions, allowing you to validate trace data generated during tests.

## Features

* Collects OpenTelemetry traces via the OTLP protocol.
* Provides an HTTP API for querying collected traces.
* Designed for use in integration testing scenarios.
* Can be deployed as a container or integrated into an Aspire project.

## Usage

1. Adding QueryableTraceCollector to Your Aspire Project

add the hosting nuget package to the 
`<PackageReference Include="AJP.Aspire.Hosting.QueryableTraceCollector" Version="1.0.0" />`

```csharp
var queryableTraceCollectorApiKey = builder.Configuration["QueryableTraceCollectorApiKey"] ?? "123456789"; // I add a local secret in VSCode...
var queryabletracecollector = builder.AddQueryableTraceCollector("queryabletracecollector", queryableTraceCollectorApiKey)
    .WithExternalHttpEndpoints()
    .ExcludeFromManifest();
```
This will add a container resource which runs the API.

2. Add the client integration nuget package, probably to ServiceDefaults
`<PackageReference Include="AJP.Aspire.QueryableTraceCollector.Client" Version="1.0.0" />`

Add

```csharp
public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        //... usual stuff omitted

        builder.AddOpenTelemetryExporters();

        builder.Services.AddQueryableOtelCollectorExporter(builder.Configuration, ["Outbox Item Insertion", "User Notication Sent", "Domain Event Insertion"]); // Filter collected trace data by DisplayName to just the bits we're interested in, these can also be wired up through config instead if you prefer...

        return builder;
    }
```

3. Assert against collected trace data in an Aspire test project

I do it with a nice framework🙂 see my test [here](../../tests/WeatherApp.Tests.Aspire.Integration/WeatherAppAspireIntegrationTests.cs)

```csharp
[Test]
public void PostWeatherData_EventuallyResultsIn_AUserNotificationBeingSent2()
{
    var (given, when, then) = (new Given(), new When(), new Then());

    given.WeHaveSetupTheAppHost(out var appHost)
        .And.WeRunTheAppHost(appHost, out var app, DefaultTimeout)
        .And.WeCreateAnHttpClientForTheQueryableTraceCollector(app, appHost.Configuration, out var queryableTraceCollectorClient)
        .And.WeClearAnyCollectedTraces(queryableTraceCollectorClient)
        .And.WeCreateAnHtppClientForTheAPI(app, out var apiHttpClient, DefaultTimeout)
        .And.WeHaveSomeCollectedWeatherData(out var location, out var reference, out var requestId, out var collectedWeatherData);

    when.WeWrapTheWeatherDataInAnHttpRequest(out var httpRequest, location, reference, requestId, collectedWeatherData)
        .And.WeSendTheRequest(apiHttpClient, httpRequest, out var response);

    then.TheResponseShouldBe(response, HttpStatusCode.OK);
            
    when.WeWaitWhilePollingForTheNotificationTrace(queryableTraceCollectorClient, 9, "User Notication Sent", out var traces);
        
    then.WeAssertAgainstTheTraces(traces, traces => 
    {
        traces.AssertContainsDomainEventInsertionTag("WeatherDataCollectionInitiated");
        traces.AssertContainsDomainEventInsertionTag("SubmittedToModeling");
        traces.AssertContainsDomainEventInsertionTag("ModelUpdated");
        
        traces.AssertContainsDisplayName("Outbox Item Insertion");

        traces.AssertContains(t => t.DisplayName == "User Notication Sent"
            && t.ContainsTag("user-notification-event.body", x => x == "Dear user, your data has been submitted and included in our latest model")
            && t.ContainsTag("user-notification-event.reference", x => x == reference), 
            "Didn't find the expected user notification trace with the expected tags.");            
    });        
}
```

4. Sit back and enjoy not having to poll databases or interogate service bus queues etc😎

## Publishing a new version of the QueryableTraceCollector

The QueryableTraceCollector is a container resource, when added to an Aspire project, a container image will be pulled from Docker Hub, if making any changes to the QueryableTraceCollector, we need to build a new Dicker image and push it...

- First log into Docker Hub and create a repository, that will give you a tag to push to. Mine is [here](https://hub.docker.com/repository/docker/andrewjpoole/queryabletracecollector/general)
- open terminal at root of repo
- build the image
`docker build -f .\aspire-experiments\QueryableTraceCollector\Dockerfile . -t andrewjpoole/queryabletracecollector`
- check it exists locally
`docker image ls`
- push to Docker registry
`docker push andrewjpoole/queryabletracecollector`