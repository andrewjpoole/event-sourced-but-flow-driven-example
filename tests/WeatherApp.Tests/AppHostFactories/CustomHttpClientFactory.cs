namespace WeatherApp.Tests.AppHostFactories;
/*
    Strategy to allow a Component to call another Component's in-memory HTTP client during an e2e component test.
    Use a custom IHttpClientFactory in which to inject other component's in-memory HttpClients to call.
*/
public class CustomHttpClientFactory() : IHttpClientFactory
{
    public Dictionary<string, HttpClient> HttpClients = [];

    public HttpClient CreateClient(string name) =>
        HttpClients.GetValueOrDefault(name)
        ?? throw new InvalidOperationException($"HTTP client is not found for client with name {name}");
}

/*
    Add the following code to WebApplicationFactory for the Component that needs to call another Component: 
    
    // The custom factory, backed by a simple Dictionary.
    private readonly CustomHttpClientFactory customHttpClientFactory = new();

    // Switch out the standard IHttpClientFactory with the custom one.
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            // Replace standard IHttpClientFactory impl with custom one with any added HTTP clients.
            services.AddSingleton<IHttpClientFactory>(customHttpClientFactory);
        });
    }

    // Methods for clearing and adding HTTP clients to the custom factory.
    public void ClearHttpClients() => customHttpClientFactory.HttpClients.Clear();

    public void AddHttpClient(string clientName, HttpClient client)
    {
        if (customHttpClientFactory.HttpClients.TryAdd(clientName, client) == false)
        {
            throw new InvalidOperationException($"HttpClient with name {clientName} is already added");
        }
    }

*/