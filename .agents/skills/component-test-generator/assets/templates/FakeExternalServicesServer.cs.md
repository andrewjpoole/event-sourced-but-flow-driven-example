# FakeExternalServicesServer template

Purpose: provide a real Kestrel `WebApplication` that handles outbound HTTP calls from apps under test. Each endpoint reads a shared `FakeServicesState` object to decide what to return, and logs every request with `Console.WriteLine` so the output is visible per test.

Create two files: `FakeExternalServices/FakeServicesState.cs` and `FakeExternalServices/FakeExternalServicesServer.cs`.

## FakeServicesState.cs

```csharp
namespace {Namespace}.Tests.TUnit.FakeExternalServices;

/// <summary>
/// Controls how FakeExternalServicesServer responds to requests.
/// Set flags in Given methods; reset in WeHaveResetEverything().
/// </summary>
public class FakeServicesState
{
    // Add one boolean per controllable external service behaviour.
    public bool {ExternalService1}Accepted { get; set; } = true;
    public bool {ExternalService2}Accepted { get; set; } = true;

    public void Reset()
    {
        {ExternalService1}Accepted = true;
        {ExternalService2}Accepted = true;
    }
}
```

## FakeExternalServicesServer.cs

```csharp
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace {Namespace}.Tests.TUnit.FakeExternalServices;

public sealed class FakeExternalServicesServer : IDisposable
{
    private readonly WebApplication _app;

    public readonly FakeServicesState State = new();

    /// <summary>Base URL including scheme and port, e.g. "http://127.0.0.1:54321".</summary>
    public string BaseUrl { get; private set; } = string.Empty;

    public FakeExternalServicesServer()
    {
        var builder = WebApplication.CreateBuilder();

        // ConfigureKestrel is the correct method on WebApplicationBuilder.WebHost.
        // Use Listen(IPAddress.Loopback, 0) for a dynamic port.
        // Do NOT use ListenLocalhost(0) — dynamic port on localhost throws at runtime.
        builder.WebHost.ConfigureKestrel(o => o.Listen(IPAddress.Loopback, 0));
        builder.Logging.ClearProviders();

        _app = builder.Build();
        MapEndpoints(_app);

        _app.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

        // Read the actual bound port after startup.
        BaseUrl = _app.Services
            .GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.First();

        Console.WriteLine($"[FakeServer] Listening on {BaseUrl}");
    }

    private void MapEndpoints(WebApplication app)
    {
        // Map one endpoint per external service operation.
        // Use route parameters to capture IDs and log them.
        // Read State flags to decide success vs error response.

        app.MapPost("/{externalService1Route}/{id}", (string id) =>
        {
            var accepted = State.{ExternalService1}Accepted;
            Console.WriteLine($"[FakeServer] POST {{{externalService1Route}}}/{id} → {(accepted ? 200 : 422)}");
            return accepted
                ? Results.Ok(new { id })
                : Results.UnprocessableEntity(new { reason = "unavailable" });
        });

        app.MapPost("/{externalService2Route}", () =>
        {
            var accepted = State.{ExternalService2}Accepted;
            Console.WriteLine($"[FakeServer] POST {{{externalService2Route}}} → {(accepted ? 200 : 402)}");
            return accepted
                ? Results.Ok(new { accepted = true })
                : Results.StatusCode(402);
        });

        // Add more endpoints to match the routes in each app's HttpClient implementation.
    }

    public void Dispose() => _app.DisposeAsync().AsTask().GetAwaiter().GetResult();
}
```

Adaptation notes:

- Discover the exact routes by reading the `HttpClient` implementation classes in `src/` (look for `_httpClient.PostAsync(...)` or `_httpClient.GetAsync(...)` calls).
- Route parameters must match the path shape the app actually constructs — inspect the concrete client class, not just the interface.
- Keep endpoint handlers thin: read `State`, log, return. Do not put business logic here.
- Add `Console.WriteLine` to every endpoint so each test output shows which external calls were made and what status was returned.
