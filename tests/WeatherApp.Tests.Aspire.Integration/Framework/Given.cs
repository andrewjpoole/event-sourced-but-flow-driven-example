using Aspire.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

namespace WeatherApp.Tests.Aspire.Integration.Framework;

public class Given
{
    public Given And => this;

    public Given WeHaveSetupTheAppHost(out IDistributedApplicationTestingBuilder appHost)
    {
        appHost = DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>()
            .GetAwaiter().GetResult();

        // We can override configuration for resources here...
        appHost.Resources.FirstOrDefault(x => x.Name == "outbox")?
            .Annotations.Add(new EnvironmentCallbackAnnotation("OutboxProcessorOptions__IntervalBetweenBatchesInSeconds", () => "2"));
    
        // https://learn.microsoft.com/en-us/dotnet/aspire/app-host/eventing
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

    public Given WeHaveSomeCollectedWeatherData(out string location, out string reference, out Guid requestId, out CollectedWeatherDataModel collectedWeatherData)
    {
        location = $"TestLocation{Random.Shared.Next(10,99)}";
        reference = $"TestReference{Random.Shared.Next(10,99)}";
        requestId = Guid.NewGuid();

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

    public Given WeAddTheDashboardService(IDistributedApplicationTestingBuilder appHost)
    {
        appHost.Services.AddDashboardWebApplication();

        return this;
    }
}
