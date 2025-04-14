using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace WeatherApp.Tests.Aspire.Integration;

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
    public async Task PostWeatherData_EventuallyResultsIn_AUserNotificationBeingSent()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();

        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
        });

        // appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        // {
        //     clientBuilder.AddStandardResilienceHandler();
        // });
    
        await using var app = await appHost.BuildAsync().WaitAsync(DefaultTimeout);
        await app.StartAsync().WaitAsync(DefaultTimeout);
    
        // Act
        var httpClient = app.CreateHttpClient("api");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("api").WaitAsync(DefaultTimeout);

        var queryableTraceCollectorClient = app.CreateHttpClient("queryabletracecollector")
            ?? throw new InvalidOperationException("Queryable Trace Collector client is not available.");

        var location = "TestLocation";
            var reference = "TestReference";
            var collectedWeatherData = new CollectedWeatherDataModel(
                new List<CollectedWeatherDataPointModel>
                    {
                        new CollectedWeatherDataPointModel(
                            DateTimeOffset.UtcNow,
                            10.5m,
                            "N",
                            25.3m,
                            60.2m)
                    });

        var body = new StringContent(
            JsonSerializer.Serialize(collectedWeatherData),
            Encoding.UTF8,
            "application/json");

        // Act
        httpClient.Timeout = TimeSpan.FromSeconds(120);
        var response = await httpClient.PostAsync(
            $"/v1/collected-weather-data/{location}/{reference}", body); // will return once the synchronous call is done...
    
        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // now poll for the notification trace...
        var notificationTraceFound = false;
        for (var i = 0; i < 10; i++)
        {
            var tracePollResponse = await queryableTraceCollectorClient.GetAsync("/traces/Outbox Item Insertion");
            if (tracePollResponse.StatusCode == HttpStatusCode.OK)            
            {
                notificationTraceFound = true;
                break;
            }
            
            await Task.Delay(1000);
        }

        if(notificationTraceFound == false)        
            Assert.Fail("Notification trace not found within the timeout period.");
        
        // now fetch all of the traces...
        var allTracesResponse = await queryableTraceCollectorClient.GetAsync("/traces");
        var tracesJson = await allTracesResponse.Content.ReadAsStringAsync();
        var traces = JsonSerializer.Deserialize<List<TraceData>>(tracesJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.That(traces, Is.Not.Null, "Traces should not be null.");
        Assert.That(traces, Is.Not.Empty, "Traces should not be empty.");
        Assert.That(traces.Count(x => x.DisplayName == "Outbox Item Insertion"), Is.EqualTo(1), "There should be one Outbox Item Insertion trace.");
        
    }
}

public record CollectedWeatherDataPointModel(
    DateTimeOffset time,
    decimal WindSpeedInMetersPerSecond,
    string WindDirection,
    decimal TemperatureReadingInDegreesCelcius,
    decimal HumidityReadingInPercent
    );

public record CollectedWeatherDataModel(List<CollectedWeatherDataPointModel> Points);

public record WeatherReportResponse(string RequestedRegion, DateTime RequestedDate, Guid RequestId, int Temperature, string Summary);

public class TraceData
{
    public string Resource { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public string SpanId { get; set; } = string.Empty;    
    public Dictionary<string, object?> Tags { get; set; } = new();
}
