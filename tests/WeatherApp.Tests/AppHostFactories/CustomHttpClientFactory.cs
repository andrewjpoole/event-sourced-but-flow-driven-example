namespace WeatherApp.Tests.AppHostFactories;

public class CustomHttpClientFactory() : IHttpClientFactory
{
    public Dictionary<string, HttpClient> HttpClients = [];

    public HttpClient CreateClient(string name) =>
        HttpClients.GetValueOrDefault(name)
        ?? throw new InvalidOperationException($"HTTP client is not found for client with name {name}");
}