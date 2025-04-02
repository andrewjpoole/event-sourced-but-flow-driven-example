using WeatherApp.Infrastructure.Persistence;
using WeatherApp.Infrastructure.Outbox;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddLogging()
    .AddDatabase(builder.Configuration)
    .AddOutboxDispatcherService();

var app = builder.Build();

app.MapGet("/", () => "Outbox Service is running!");

app.Run();
