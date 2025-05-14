using Microsoft.AspNetCore.Mvc;

namespace Aspire.QueryableTraceCollector.Integration;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
            .AddSingleton<CollectedData>();
        
        var app = builder.Build();

        var logger = app.Services.GetRequiredService<ILogger<Program>>();

        var apiKey = builder.Configuration["ApiKey"] ?? 
            throw new Exception("ApiKey is not set in the configuration.");

        logger.LogInformation("Using ApiKey {ApiKey}", apiKey);

        app.Use(async (context, next) =>
        {
            if(context.Request.Path != "/traces")
            {
                await next();
                return;
            }

            if (!context.Request.Headers.TryGetValue("X-Api-Key", out var extractedApiKey))
            {
                logger.LogWarning("API Key is missing, expected a Request Header named X-Api-Key.");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("API Key is missing.");
                return;
            }
            
            if (apiKey.Equals(extractedApiKey) != true)
            {
                logger.LogWarning("Unauthorized client, provided ApiKey {ProvvidedApiKey}, required ApiKey {RequiredApiKey}.", extractedApiKey, apiKey);
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Unauthorized client.");
                return;
            }

            await next();
        });

        app.MapGet("/", () => "Queryable Trace Collector is running!");

        app.MapPost("/traces", ([FromServices]CollectedData repository, List<ActivityLite> data) => 
        {            
            repository.Import(data);
            return Results.Ok("Trace data batch added.");
        });

        app.MapGet("/traces", ([FromServices]CollectedData repository) => repository.GetTraces());        

        app.MapGet("/traces/{displayName}", ([FromServices]CollectedData repository, string displayName) => 
            repository.GetTrace(displayName));

        app.MapGet("/traces/count", ([FromServices]CollectedData repository) => repository.Count());

        app.MapDelete("/traces", ([FromServices]CollectedData repository) => 
        {
            repository.Clear();
            return Results.Ok("Traces cleared.");
        });

        app.Run();
    }
}