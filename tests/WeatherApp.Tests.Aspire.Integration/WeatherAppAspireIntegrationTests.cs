using Microsoft.Extensions.Logging;
using WeatherApp.Tests.Aspire.Integration.Framework;

namespace WeatherApp.Tests.Aspire.Integration;

// https://learn.microsoft.com/en-us/dotnet/aspire/testing/accessing-resources?pivots=xunit
public class WeatherAppAspireIntegrationTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);
    
    [Test]
    public async Task GetWebResourceRootReturnsOkStatusCode()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            // Override the logging filters from the app's configuration
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
        });
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
    
        await using var app = await appHost.BuildAsync().WaitAsync(DefaultTimeout);
        await app.StartAsync().WaitAsync(DefaultTimeout);
    
        // Act
        var httpClient = app.CreateHttpClient("api");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("api").WaitAsync(DefaultTimeout);
        var response = await httpClient.GetAsync("/");
    
        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // This default test passes once the resource names are changed, you can observe the dependency containers coming up and down in docker desktop!
    }

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
}
