using WeatherApp.Application.Handlers;
using WeatherApp.Application.Models.IntegrationEvents.WeatherModelingEvents;
using WeatherApp.Application.Orchestration;
using WeatherApp.Application.Services;
using WeatherApp.Domain.ServiceDefinitions;
using WeatherApp.Infrastructure.ApiClients.WeatherModelingSystem;
using WeatherApp.Infrastructure.ApiClients.ContributorPaymentsService;
using WeatherApp.Infrastructure.ApiClients;
using WeatherApp.Infrastructure.LocationManager;
using WeatherApp.Infrastructure.MessageBus;
//using WeatherApp.Infrastructure.Notifications;
using WeatherApp.Infrastructure.Persistence;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using WeatherApp.Infrastructure.Outbox;

namespace WeatherApp.EventListener;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddEnvironmentVariables(prefix: "WeatherApp_");
        builder.Configuration.AddEnvironmentVariables(prefix: "WeatherApp_Listener_");

        builder.Logging
            .ClearProviders()
            .AddConsole();

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(builder.Environment.ApplicationName))
            .WithLogging(logging => logging
                .AddOtlpExporter());
        
        var config = builder.Configuration;
        
        builder.Services
            .AddDatabase(config)
            .AddEventSourcing()
            .AddServiceBusInboundQueueHandlerOptions(config)
            .ConfigureServiceBusClient(config)
            .AddOutboxServices()
            .AddSingleton(x => TimeProvider.System)
            .AddSingleton<IGetWeatherReportRequestHandler, GetWeatherReportRequestHandler>()
            .AddSingleton<ISubmitWeatherDataCommandHandler, CollectedWeatherDataOrchestrator>()
            .AddSingleton<IRegionValidator, RegionValidator>()
            .AddSingleton<IDateChecker, DateChecker>()
            .AddSingleton<IWeatherForecastGenerator, WeatherForecastGenerator>()
            .AddSingleton<IEventPersistenceService, EventPersistenceService>()
            .AddSingleton<IEventRepository, EventRepositoryInMemory>()
            .AddSingleton<IWeatherDataValidator, WeatherDataValidator>()
            .AddSingleton<ILocationManager, LocationManager>()
            .AddContributorPaymentsService(config)
            .AddWeatherModelingService(config)
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