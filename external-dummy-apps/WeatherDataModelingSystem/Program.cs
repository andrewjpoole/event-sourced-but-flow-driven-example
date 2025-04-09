using Microsoft.AspNetCore.Mvc;
using WeatherApp.Application.Models.IntegrationEvents.WeatherModelingEvents;
using WeatherApp.Domain.Entities;
using WeatherApp.Infrastructure.Messaging;

namespace WeatherDataModelingSystem;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();   
        builder.AddAzureServiceBusClient(connectionName: "asb");

        builder.Services
            .AddSingleton(x => TimeProvider.System)
            .AddServiceBusOutboundEntityOptions(builder.Configuration)
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
        
        app.MapPost("/v1/collected-weather-data/{location}/{streamId}", async (string location, Guid streamId, [FromBody]CollectedWeatherData payload, HttpContext context) => 
           {
                logger.LogInformation("Received collected weather data for location: {Location}, submissionId: {SubmissionId}", location, streamId);

                await Task.Delay(5_000); // Simulate some processing time
                
                var modelingDataAcceptedIntegrationEvent = new ModelingDataAcceptedIntegrationEvent(streamId);
                await modelingDataAcceptedIntegrationEventSender.SendAsync(modelingDataAcceptedIntegrationEvent);
                logger.LogInformation("ModelingDataAcceptedIntegrationEvent sent for location: {Location}, submissionId: {SubmissionId}", location, streamId);

                // Don't await, but after some time simulate sending the ModelUpdatedEvent...
                _ = Task.Run(async () => 
                {
                    await Task.Delay(10_000);
                    logger.LogInformation("Simulating model update for location: {Location}, submissionId: {SubmissionId}", location, streamId);
                    var modelUpdatedIntegrationEvent = new ModelUpdatedIntegrationEvent(streamId);
                    await modelUpdatedIntegrationEventSender.SendAsync(modelUpdatedIntegrationEvent);
                    logger.LogInformation("ModelUpdatedIntegrationEvent sent for location: {Location}, submissionId: {SubmissionId}", location, streamId);
                });

                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(Guid.NewGuid().ToString()); // return submission ID as response.

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