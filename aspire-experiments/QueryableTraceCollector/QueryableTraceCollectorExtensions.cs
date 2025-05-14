using Aspire.Hosting.ApplicationModel;
using WeatherApp.Aspire.Integration;

namespace Aspire.Hosting;

public static class QueryableTraceCollectorResourceExtensions
{
    /// <summary>
    /// Adds the <see cref="QueryableTraceCollectorResource"/> to the given
    /// <paramref name="builder"/> instance.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns>
    /// An <see cref="IResourceBuilder{QueryableTraceCollectorResource}"/> instance that
    /// represents the added QueryableTraceCollector resource.
    /// </returns>
    public static IResourceBuilder<QueryableTraceCollectorResource> AddQueryableTraceCollector(
        this IDistributedApplicationBuilder builder,
        string name,
        string? apiKey = null,
        int httpPort = 8000)
    {
        // The AddResource method is a core API within .NET Aspire and is
        // used by resource developers to wrap a custom resource in an
        // IResourceBuilder<T> instance. Extension methods to customize
        // the resource (if any exist) target the builder interface.
        var resource = new QueryableTraceCollectorResource(name);

        apiKey ??= Guid.NewGuid().ToString()[..8];

        return builder.AddResource(resource)
                    .WithImage("andrewjpoole/queryabletracecollector")
                    .WithImageRegistry( "docker.io")
                    .WithHttpEndpoint(port: httpPort, targetPort: 8080)
                    .WithArgs($"--api-key {apiKey}");
    }
}