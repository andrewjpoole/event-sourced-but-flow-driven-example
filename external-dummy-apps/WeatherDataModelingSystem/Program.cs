using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using WeatherApp.Application.Models.IntegrationEvents.WeatherModelingEvents;
using WeatherApp.Domain.Entities;
using WeatherApp.Infrastructure.MessageBus;

namespace WeatherDataModelingSystem;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddEnvironmentVariables(prefix: "WeatherApp_");
        builder.Configuration.AddEnvironmentVariables(prefix: "WeatherApp_WDMS_");

        builder.Logging
            .ClearProviders()
            .AddConsole();

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(builder.Environment.ApplicationName))
            .WithLogging(logging => logging
                .AddOtlpExporter());

        builder.Services
            .AddSingleton(x => TimeProvider.System)
            .AddServiceBusOutboundEntityOptions(builder.Configuration)
            .ConfigureServiceBusClient(builder.Configuration)
            .AddSingleton<IMessageSender<ModelingDataAcceptedIntegrationEvent>, MessageSender<ModelingDataAcceptedIntegrationEvent>>()
            .AddSingleton<IMessageSender<ModelingDataRejectedIntegrationEvent>, MessageSender<ModelingDataRejectedIntegrationEvent>>()
            .AddSingleton<IMessageSender<ModelUpdatedIntegrationEvent>, MessageSender<ModelUpdatedIntegrationEvent>>();

        //builder.WebHost.ConfigureKestrel((context, serverOptions) => serverOptions.Listen(IPAddress.Loopback, 5001));
        
        var app = builder.Build();
        
        app.UseHttpsRedirection();

        app.MapGet("/", () => "Weather Data Modeling System is running!");
        
        app.MapPost("/v1/collected-weather-data/{location}/{submissionId}", async (string location, Guid submissionId, [FromBody]CollectedWeatherData payload, HttpContext context) => 
           {
                var modelingDataAcceptedIntegrationEventSender = context.RequestServices.GetRequiredService<IMessageSender<ModelingDataAcceptedIntegrationEvent>>();
                var modelingDataAcceptedIntegrationEvent = new ModelingDataAcceptedIntegrationEvent(submissionId);
                await modelingDataAcceptedIntegrationEventSender.SendAsync(modelingDataAcceptedIntegrationEvent);

                // Don't await, but after some time simulate sending the ModelUpdatedEvent...
                _ = Task.Run(async () => 
                {
                    await Task.Delay(10_000);
                    var modelingDataAcceptedIntegrationEventSender = context.RequestServices.GetRequiredService<IMessageSender<ModelUpdatedIntegrationEvent>>();
                    var modelUpdatedIntegrationEvent = new ModelUpdatedIntegrationEvent(submissionId);
                    await modelingDataAcceptedIntegrationEventSender.SendAsync(modelUpdatedIntegrationEvent);
                });

                context.Response.StatusCode = StatusCodes.Status200OK;
                return;
           });
        
        await app.RunAsync();
    }
}