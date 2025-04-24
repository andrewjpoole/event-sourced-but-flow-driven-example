using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;

namespace WeatherApp.Aspire.Integration;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
            .AddSingleton(new CollectedData());
        
        var app = builder.Build();
                

        app.MapGet("/", () => "Queryable Trace Collector is running!");

        app.MapPost("/traces", ([FromServices]CollectedData repository, List<TraceData> data) => 
        {
            repository.Import(data);
            return Results.Ok("Trace data batch added.");
        });

        app.MapGet("/traces", ([FromServices]CollectedData repository) => 
            repository.GetTraces());        

        app.MapDelete("/traces", ([FromServices]CollectedData repository) => 
        {
            repository.Clear();
            return Results.Ok("Traces cleared.");
        });

        app.Run();
    }
}

public class CollectedData
{
    private readonly ConcurrentBag<TraceData> activities = new();

    public void Import(in List<TraceData> batch)
    {
        foreach (var activity in batch)
        {
            activities.Add(activity);
        }
    }

    public IReadOnlyCollection<TraceData> GetTraces() => activities.ToArray();

    public void Clear() => activities.Clear();    
}
