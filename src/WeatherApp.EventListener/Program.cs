using WeatherApp.Application.Handlers;
using WeatherApp.Application.Models.IntegrationEvents.WeatherModelingEvents;
using WeatherApp.Application.Orchestration;
using WeatherApp.Application.Services;
using WeatherApp.Infrastructure.ApiClients.WeatherModelingSystem;
using WeatherApp.Infrastructure.LocationManager;
using WeatherApp.Infrastructure.MessageBus;
using WeatherApp.Infrastructure.Persistence;
using WeatherApp.Infrastructure.Outbox;
using WeatherApp.Infrastructure.ApiClientWrapper;
using WeatherApp.Infrastructure.ContributorPayments;

namespace WeatherApp.EventListener;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();
        builder.AddAzureServiceBusClient(connectionName: "asb");
        builder.AddSqlServerClient(connectionName: "WeatherAppDb");
        
        var config = builder.Configuration;
        
        builder.Services
            .AddDatabaseConnectionFactory()
            .AddEventSourcing()
            .AddServiceBusInboundQueueHandlerOptions(config)
            .AddServiceBusOutboundEntityOptions(config)
            .AddOutboxServices()
            .AddSingleton(x => TimeProvider.System)
            .AddSingleton<IGetWeatherReportRequestHandler, GetWeatherReportRequestHandler>()
            .AddSingleton<ISubmitWeatherDataCommandHandler, CollectedWeatherDataOrchestrator>()
            .AddSingleton<IRegionValidator, RegionValidator>()
            .AddSingleton<IDateChecker, DateChecker>()
            .AddSingleton<IWeatherForecastGenerator, WeatherForecastGenerator>()  
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

        app.MapGet("/", () => "Weather App Event Listener is running!");

        await app.RunAsync();
    }
}