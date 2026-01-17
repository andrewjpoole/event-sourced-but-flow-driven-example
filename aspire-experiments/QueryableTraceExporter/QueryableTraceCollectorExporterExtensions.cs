using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

public static class QueryableTraceCollectorExporterExtensions
{

    public static IServiceCollection AddQueryableOtelCollectorExporter(
        this IServiceCollection services, IConfiguration config, List<string>? allowedActivityDisplayNames = null)
    {   
        var baseAddress = config["services:queryabletracecollector:http:0"];
        if(baseAddress == null)
            return services; // just return if the relationship is not specified in AppHost.

        services.AddHttpClient("QueryableTraceCollector", client =>
        {   
            client.BaseAddress = new(baseAddress);
            client.DefaultRequestHeaders.Add("X-Api-Key", config["QueryableTraceCollectorApiKey"] ?? "123456789");
        }).ConfigurePrimaryHttpMessageHandler(() => 
        {
            var handler = new HttpClientHandler();
            return new ClientHandlerWithTracingDisabled(handler);
        });

        if(allowedActivityDisplayNames == null)
            allowedActivityDisplayNames = config.GetSection("QueryableTraceCollector:AllowedActivityDisplayNames")
                .Get<List<string>>() ?? new List<string>();

        var appName = Path.GetFileName(Process.GetCurrentProcess().MainModule?.FileName.Replace(".exe", string.Empty) ?? "Unknown");
        
        services.AddOpenTelemetry().WithTracing(traceBuilder =>
        {
            traceBuilder.AddProcessor(services => 
                new SimpleActivityExportProcessor(new QueryableTraceCollectorExporter(appName, services, allowedActivityDisplayNames)));
        });        

        return services;
    }
}
