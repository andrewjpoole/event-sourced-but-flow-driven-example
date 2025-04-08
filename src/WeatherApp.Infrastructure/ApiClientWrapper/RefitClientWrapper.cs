using System.Text.Json;
using Refit;

namespace WeatherApp.Infrastructure.ApiClientWrapper;

public class RefitClientWrapper<T>(IHttpClientFactory clientFactory) : IRefitClientWrapper<T>
{
    private readonly IHttpClientFactory clientFactory = clientFactory;

    public T CreateClient()
    {
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var typeOfT = typeof(T);
        var nameofT = typeOfT.FullName ?? typeOfT.Name;

        var client = clientFactory.CreateClient(nameofT);
        if (client == null)
            throw new ArgumentNullException(nameof(client), $"HttpClient with name {nameofT} not found.");
            
        var refitClient = RestService.For<T>(client, new RefitSettings(new SystemTextJsonContentSerializer(jsonSerializerOptions)));

        return refitClient;
    }
}