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
        
        var app = builder.Build();
        
        app.UseHttpsRedirection();

        // Get services needed for handling requests
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        var modelingDataAcceptedIntegrationEventSender = app.Services.GetRequiredService<IMessageSender<ModelingDataAcceptedIntegrationEvent>>();
        var modelingDataRejectedIntegrationEventSender = app.Services.GetRequiredService<IMessageSender<ModelingDataRejectedIntegrationEvent>>();
        var modelUpdatedIntegrationEventSender = app.Services.GetRequiredService<IMessageSender<ModelUpdatedIntegrationEvent>>();

        app.MapGet("/", () => "Weather Data Modeling System is running!");
        
        app.MapPost("/v1/collected-weather-data/{location}/{submissionId}", async (string location, Guid submissionId, [FromBody]CollectedWeatherData payload, HttpContext context) => 
           {
                logger.LogInformation("Received collected weather data for location: {Location}, submissionId: {SubmissionId}", location, submissionId);

                await Task.Delay(5_000); // Simulate some processing time
                
                var modelingDataAcceptedIntegrationEvent = new ModelingDataAcceptedIntegrationEvent(submissionId);
                await modelingDataAcceptedIntegrationEventSender.SendAsync(modelingDataAcceptedIntegrationEvent);
                logger.LogInformation("ModelingDataAcceptedIntegrationEvent sent for location: {Location}, submissionId: {SubmissionId}", location, submissionId);

                // Don't await, but after some time simulate sending the ModelUpdatedEvent...
                _ = Task.Run(async () => 
                {
                    await Task.Delay(10_000);
                    logger.LogInformation("Simulating model update for location: {Location}, submissionId: {SubmissionId}", location, submissionId);
                    var modelUpdatedIntegrationEvent = new ModelUpdatedIntegrationEvent(submissionId);
                    await modelUpdatedIntegrationEventSender.SendAsync(modelUpdatedIntegrationEvent);
                    logger.LogInformation("ModelUpdatedIntegrationEvent sent for location: {Location}, submissionId: {SubmissionId}", location, submissionId);
                });

                context.Response.StatusCode = StatusCodes.Status200OK;
                return;
           });

        app.MapPost("/test", async (HttpContext context) => 
        {   
            var path = context.Request.Path.ToString();
            var query = context.Request.QueryString.ToString();
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();

            logger.LogInformation("Received test request: Path: {Path}, Query: {Query}, Body: {Body}", path, query, body);
            
            return Results.Ok("Test event sent.");
        });
        
        await app.RunAsync();
    }
}