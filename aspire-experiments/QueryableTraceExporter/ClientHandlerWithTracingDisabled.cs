using System.Diagnostics;

namespace Microsoft.Extensions.Hosting;

public class ClientHandlerWithTracingDisabled : DelegatingHandler
{
    public ClientHandlerWithTracingDisabled(HttpMessageHandler innerHandler) : base(innerHandler){}

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Activity.Current = null;
        return await base.SendAsync(request, cancellationToken);
    }

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Activity.Current = null;
        return base.Send(request, cancellationToken);
    }
}
