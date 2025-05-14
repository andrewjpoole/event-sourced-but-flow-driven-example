using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

public static class QueryableTraceCollectorExporterExtensions
{

    public static IServiceCollection AddQueryableOtelCollectorExporter(this IServiceCollection services, IConfiguration config, List<string>? allowedActivityDisplayNames = null)
    {   
        if(allowedActivityDisplayNames == null)
            allowedActivityDisplayNames = config.GetSection("QueryableTraceCollector:AllowedActivityDisplayNames").Get<List<string>>() ?? new List<string>();

        var appName = Path.GetFileName(Process.GetCurrentProcess().MainModule?.FileName.Replace(".exe", string.Empty) ?? "Unknown");
        var exporter = new QueryableTraceCollectorExporter(appName, config, allowedActivityDisplayNames);

        // TODO configure httpClient/factory here and add to IoC?

        services.AddOpenTelemetry().WithTracing(traceBuilder =>
        {                        
            traceBuilder.AddProcessor(new SimpleActivityExportProcessor(exporter));
        });        

        return services;
    }
}
