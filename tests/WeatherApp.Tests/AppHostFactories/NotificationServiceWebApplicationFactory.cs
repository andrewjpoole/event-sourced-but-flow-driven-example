using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WeatherApp.NotificationAPI;

namespace WeatherApp.Tests.AppHostFactories;

public class NotificationServiceWebApplicationFactory : WebApplicationFactory<NotificationAPI.Program>
{
    public NotificationHandler? NotificationHandler;
    public HttpClient? HttpClient;
    private IHost? host;

    protected override IHost CreateHost(IHostBuilder builder)
    {
        host = base.CreateHost(builder);

        // Get a reference to the real NotificationHandle, so we can use it for assertions.
        NotificationHandler = (NotificationHandler)host.Services.GetRequiredService<INotificationHandler>(); 

        return host;
    }

    public void Start()
    {
        HttpClient = CreateClient();
    }
}

