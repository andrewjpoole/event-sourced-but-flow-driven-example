using Microsoft.Extensions.Options;
using WeatherApp.Application.Handlers;
using WeatherApp.Application.Models.IntegrationEvents.WeatherModelingEvents;
using WeatherApp.Application.Orchestration;
using WeatherApp.Application.Services;
using WeatherApp.Domain.ServiceDefinitions;
using WeatherApp.Infrastructure.ApiClients;
using WeatherApp.Infrastructure.ApiClients.WeatherModelingSystem;
using WeatherApp.Infrastructure.ContributorPayments;
using WeatherApp.Infrastructure.LocationManager;
using WeatherApp.Infrastructure.MessageBus;
using WeatherApp.Infrastructure.Notifications;
using WeatherApp.Infrastructure.Persistence;

namespace WeatherApp.EventListener.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static void ConfigureServices(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var config = builder.Configuration;

        //services.Configure<ServiceBusInboundQueueHandlerOptions>(config.GetSection(ServiceBusInboundQueueHandlerOptions.Name));

        var queueHandlerOptions = config.GetSection(ServiceBusInboundQueueHandlerOptions.Name).Get<ServiceBusInboundQueueHandlerOptions>();
        IOptions<ServiceBusOptions> inboundServiceBusOptions = Options.Create(queueHandlerOptions);
        services.AddSingleton(inboundServiceBusOptions);

        services.ConfigureServiceBusClient(config);
        
        services
            .AddSingleton<IGetWeatherReportRequestHandler, GetWeatherReportRequestHandler>()
            .AddSingleton<IPostWeatherDataHandler, CollectedWeatherDataOrchestrator>()
            .AddSingleton<IRegionValidator, RegionValidator>()
            .AddSingleton<IDateChecker, DateChecker>()
            .AddSingleton<IWeatherForecastGenerator, WeatherForecastGenerator>()
            .AddSingleton<IEventPersistenceService, EventPersistenceService>()
            .AddSingleton<IEventRepository, EventRepository>()
            .AddSingleton<INotificationService, NotificationService>()
            .AddSingleton<IWeatherDataValidator, WeatherDataValidator>()
            .AddSingleton<ILocationManager, LocationManager>()
            .AddSingleton<IContributorPaymentService, ContributorPaymentService>()
            .AddWeatherModelingService(builder.Configuration.GetSection(WeatherModelingServiceOptions.ConfigSectionName).Get<WeatherModelingServiceOptions>())
            .AddSingleton(typeof(IRefitClientWrapper<>), typeof(RefitClientWrapper<>));

        services.AddHostedServiceBusEventListener<ModelingDataAcceptedIntegrationEvent, CollectedWeatherDataOrchestrator>();
        services.AddHostedServiceBusEventListener<ModelingDataRejectedIntegrationEvent, CollectedWeatherDataOrchestrator>();
        services.AddHostedServiceBusEventListener<ModelUpdatedIntegrationEvent, CollectedWeatherDataOrchestrator>();
    }
}