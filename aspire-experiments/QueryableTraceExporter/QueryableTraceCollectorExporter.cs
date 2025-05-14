using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using OpenTelemetry;

namespace Microsoft.Extensions.Hosting;

public class QueryableTraceCollectorExporter(string appName, IConfiguration config, List<string>? allowedActivityDisplayNames = null) : BaseExporter<Activity>
{
    public override ExportResult Export(in Batch<Activity> batch)
    {
        var baseAddress = config["services:queryabletracecollector:http:0"];
        if(baseAddress == null)
            return ExportResult.Success; // just return if the relationship is not specified in AppHost.

        using var client = new HttpClient
        {
            BaseAddress = new(baseAddress)
        };
        
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
            Headers = {{ "X-Api-Key", config["QueryableTraceCollectorApiKey"] ?? "123456789" }},
            Content = new StringContent(JsonSerializer.Serialize(traceDataCollection), System.Text.Encoding.UTF8, "application/json")
        };
        
        var response = client.Send(request);
        
        return ExportResult.Success;
    }
}
