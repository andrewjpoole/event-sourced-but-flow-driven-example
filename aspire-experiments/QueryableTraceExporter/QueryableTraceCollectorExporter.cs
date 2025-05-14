using System.Diagnostics;
using System.Text.Json;
using OpenTelemetry;

namespace Microsoft.Extensions.Hosting;

public class QueryableTraceCollectorExporter(string appName, IServiceProvider services, List<string>? allowedActivityDisplayNames = null) : BaseExporter<Activity>
{
    public override ExportResult Export(in Batch<Activity> batch)
    {        
        var httpClientFactory = services.GetService(typeof(IHttpClientFactory)) as IHttpClientFactory 
            ?? throw new Exception("IHttpClientFactory not found in DI container.");
            
        using var client = httpClientFactory.CreateClient("QueryableTraceCollector");
        
        var traceDataCollection = new List<ActivityLite>();
        foreach (var activity in batch)
        {
            if(activity == null       
            || activity.DisplayName.Contains("POST")) // avoid infinite loop. TODO: match on DisplayName/url as well?
                continue;

            if(allowedActivityDisplayNames != null 
                && allowedActivityDisplayNames.Contains(activity.DisplayName) == false)
                continue;

            var traceData = new ActivityLite
            {
                StartTimeUtc = activity.StartTimeUtc,
                Resource = appName,
                Source = activity.Source.Name,
                DisplayName = activity.DisplayName,
                TraceId = activity.TraceId.ToHexString(),
                SpanId = activity.SpanId.ToHexString(),
                Tags = activity.TagObjects.ToDictionary(k => k.Key, v => v.Value)                
            };
            traceDataCollection.Add(traceData);
        }
        if(traceDataCollection.Count == 0)
            return ExportResult.Success;

        var request = new HttpRequestMessage(HttpMethod.Post, "/traces")
        {
            Content = new StringContent(JsonSerializer.Serialize(traceDataCollection), System.Text.Encoding.UTF8, "application/json")
        };
        
        var response = client.Send(request);
        
        return ExportResult.Success;
    }
}