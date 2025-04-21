using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService;
using WeatherApp.Infrastructure.Messaging;

namespace WeatherApp.Tests.AppHostFactories;

public class NotificationServiceWebApplicationFactory(ComponentTestFixture fixture) : WebApplicationFactory<NotificationService.Program>
{    
    public HttpClient? HttpClient;
    private IHost? host;
    public readonly Mock<ILogger> MockLogger = new();

    public SentNotifications? RealSentNotifications;

    protected override IHost CreateHost(IHostBuilder builder)
    {
        Environment.SetEnvironmentVariable($"{ServiceBusInboundOptions.SectionName}__{nameof(ServiceBusOutboundOptions.Entities)}__{nameof(EntityNames.UserNotificationEvent)}", EntityNames.UserNotificationEvent);

        builder            
            .ConfigureServices(services =>
            {
                var loggerFactory = new Mock<ILoggerFactory>();
                loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);
                services.AddSingleton(loggerFactory.Object);

                services.AddSingleton<TimeProvider>(fixture.FakeTimeProvider);

                fixture.FakeServiceBus.WireUpSendersAndProcessors(services);
            })
            // As the NotificationService is a worker service using the generic Host, 
            // we need to add a WebHost in order to test it with Microsoft.AspNetCore.Mvc.Testing.
            .ConfigureWebHost(webBuilder => 
            {
                webBuilder
                    .UseTestServer()
                    .Configure(app => {});
                
            });

        host = base.CreateHost(builder);

        // Get reference to the real SentNotifications instance for assertions 
        RealSentNotifications = host.Services.GetRequiredService<SentNotifications>();

        return host;
    }

    public void Start()
    {
        HttpClient = CreateClient();
    }
}

