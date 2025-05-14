using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace WeatherApp.Aspire.Integration;

// ProjectResource, ParameterResource, ContainerResource or ExecutableResource
public sealed class QueryableTraceCollectorResource(string name) : ContainerResource(name), IResourceWithServiceDiscovery
{
}

