using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using OpenTelemetry;

namespace Microsoft.Extensions.Hosting;

public class QueryableTraceCollectorExporter(string appName, IConfiguration config, List<string> allowedActivityDisplayNames) : BaseExporter<Activity>
{
    public override ExportResult Export(in Batch<Activity> batch)
    {
        var baseAddress = config["services:queryabletracecollector:https:0"];
        if(baseAddress == null)
            return ExportResult.Success; // just return if the relationship is not specified in AppHost.

        using var client = new HttpClient
        {
            BaseAddress = new(baseAddress)
        };
        
        var traceDataCollection = new List<TraceData>();
        foreach (var activity in batch)
        {
            if(activity == null       
            || activity.DisplayName.Contains("POST")) // avoid infinite loop.
                continue;

            if(allowedActivityDisplayNames.Contains(activity.DisplayName) == false)
                continue;

            var traceData = new TraceData
            {
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

public class TraceData
{
    public string Resource { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public string SpanId { get; set; } = string.Empty;    
    public Dictionary<string, object?> Tags { get; set; } = new();
}
