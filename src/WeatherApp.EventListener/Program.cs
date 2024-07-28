using Microsoft.Extensions.Options;
using WeatherApp.Application.Handlers;
using WeatherApp.Application.Models.IntegrationEvents.WeatherModelingEvents;
using WeatherApp.Application.Orchestration;
using WeatherApp.Application.Services;
using WeatherApp.Domain.ServiceDefinitions;
using WeatherApp.Infrastructure.ApiClients.NotificationService;
using WeatherApp.Infrastructure.ApiClients.WeatherModelingSystem;
using WeatherApp.Infrastructure.ApiClients;
using WeatherApp.Infrastructure.ContributorPayments;
using WeatherApp.Infrastructure.LocationManager;
using WeatherApp.Infrastructure.MessageBus;
using WeatherApp.Infrastructure.Notifications;
using WeatherApp.Infrastructure.Persistence;

namespace WeatherApp.EventListener;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var config = builder.Configuration;

        var queueHandlerOptions = config.GetSection(ServiceBusInboundQueueHandlerOptions.Name).Get<ServiceBusInboundQueueHandlerOptions>() ??
                                  throw new Exception($"A {nameof(ServiceBusInboundQueueHandlerOptions)} config section is required.");


        IOptions<ServiceBusOptions> inboundServiceBusOptions = Options.Create(queueHandlerOptions);
        builder.Services.AddSingleton(inboundServiceBusOptions);
        builder.Services.ConfigureServiceBusClient(config);

        var weatherModelingOptions = config.GetSection(WeatherModelingServiceOptions.ConfigSectionName).Get<WeatherModelingServiceOptions>() ??
                                    throw new Exception($"A {nameof(WeatherModelingServiceOptions)} config section is required.");

        var notificationOptions = config.GetSection(NotificationsServiceOptions.ConfigSectionName).Get<NotificationsServiceOptions>() ??
                                  throw new Exception($"A {nameof(NotificationsServiceOptions)} config section is required.");

        builder.Services
            .AddSingleton<IGetWeatherReportRequestHandler, GetWeatherReportRequestHandler>()
            .AddSingleton<ISubmitWeatherDataCommandHandler, CollectedWeatherDataOrchestrator>()
            .AddSingleton<IRegionValidator, RegionValidator>()
            .AddSingleton<IDateChecker, DateChecker>()
            .AddSingleton<IWeatherForecastGenerator, WeatherForecastGenerator>()
            .AddSingleton<IEventPersistenceService, EventPersistenceService>()
            .AddSingleton<IEventRepository, EventRepositoryInMemory>()
            .AddSingleton<INotificationService, NotificationService>()
            .AddSingleton<IWeatherDataValidator, WeatherDataValidator>()
            .AddSingleton<ILocationManager, LocationManager>()
            .AddSingleton<IContributorPaymentService, ContributorPaymentService>()
            .AddWeatherModelingService(weatherModelingOptions)
            .AddNotificationsService(notificationOptions)
            .AddSingleton(typeof(IRefitClientWrapper<>), typeof(RefitClientWrapper<>));

        builder.Services
            .AddHostedServiceBusEventListener<ModelingDataAcceptedIntegrationEvent, CollectedWeatherDataOrchestrator>()
            .AddHostedServiceBusEventListener<ModelingDataRejectedIntegrationEvent, CollectedWeatherDataOrchestrator>()
            .AddHostedServiceBusEventListener<ModelUpdatedIntegrationEvent, CollectedWeatherDataOrchestrator>();

        var app = builder.Build();

        app.MapGet("/", () => "Hello World!");

        await app.RunAsync();
    }
}