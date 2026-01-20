---
layout: default
---

# How? #6 Re-wire Http clients if needed🌐

---
layout: default
---

# How? #6 Re-wire Http clients if needed🌐

### The AppHostFactory's HttpClient used to be in-memory only...

```csharp
public class CustomHttpClientFactory() : IHttpClientFactory
{
    public Dictionary<string, HttpClient> HttpClients = [];

    public HttpClient CreateClient(string name) =>
        HttpClients.GetValueOrDefault(name)
        ?? throw new InvalidOperationException($"HTTP client is not found for client with name {name}");

    // Then switch out the real HttpClentFactory with this one
    // in the calling ApplicationhostFactory... 
    // doable but quite confusing
}
```

---
layout: default
---

# How? #6 Re-wire Http clients if needed🌐

### But since .NET 10 Microsoft.AspNetCore.Mvc.Testing now lets us use Kestrel to listen on a real port!😁

```csharp

public class FactoryHttpClientExposed : WebApplicationFactory<SomeExecutable.Program>
{
    public FactoryHttpClientExposed(ComponentTestFixture fixture)
    {
        UseKestrel(x => x.ListenLocalhost(12345));
        // We get all the usual Kestrel goodness🙂.
    }
}

```