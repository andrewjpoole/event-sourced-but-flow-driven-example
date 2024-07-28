using Microsoft.AspNetCore.Mvc;
using WeatherApp.Application.Handlers;
using WeatherApp.Application.Models.Requests;
using WeatherApp.Application.Orchestration;
using WeatherApp.Application.Services;
using WeatherApp.Domain.Outcomes;
using WeatherApp.Domain.ServiceDefinitions;
using WeatherApp.Infrastructure.ApiClients;
using WeatherApp.Infrastructure.ApiClients.WeatherModelingSystem;
using WeatherApp.Infrastructure.ContributorPayments;
using WeatherApp.Infrastructure.LocationManager;
using WeatherApp.Infrastructure.Notifications;
using WeatherApp.Infrastructure.Persistence;

namespace WeatherApp.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container
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
            .AddWeatherModelingService(builder.Configuration.GetSection(WeatherModelingServiceOptions.ConfigSectionName).Get<WeatherModelingServiceOptions>()!)
            .AddSingleton(typeof(IRefitClientWrapper<>), typeof(RefitClientWrapper<>));

        var app = builder.Build();

        // Configure the HTTP request pipeline.

        app.UseHttpsRedirection();

        app.MapGet("/v1/weather-forecast/{region}/{date}", (
            [FromRoute] string region,
            [FromRoute] DateTime date,
            [FromServices] IGetWeatherReportRequestHandler handler)
            => CreateResponseFor(() => handler.HandleGetWeatherReport(region, date)));

        #region
        app.MapPost("/v1/collected-weather-data/{location}", (
            [FromRoute] string location,
            [FromBody] CollectedWeatherDataModel data,
            [FromServices] ISubmitWeatherDataCommandHandler handler,
            [FromServices] IWeatherDataValidator weatherDataValidator,
            [FromServices] ILocationManager locationManager)
            => CreateResponseFor(() => handler.HandleSubmitWeatherDataCommand(location, data, weatherDataValidator, locationManager)));
        #endregion

        static async Task<IResult> CreateResponseFor<TSuccess>(Func<Task<OneOf<TSuccess, Failure>>> handleRequestFunc)
        {
            var result = await handleRequestFunc.Invoke();
            return result.Match(
                success => Results.Ok(success),
                failure => failure.Match(
                    invalidRequestFailure => Results.BadRequest(invalidRequestFailure.ToValidationProblemDetails()),
                    unsupportedRegionFailure => Results.UnprocessableEntity(unsupportedRegionFailure.ToProblemDetails()),
                    modelingServiceRejectionFailure => Results.UnprocessableEntity(modelingServiceRejectionFailure.Message)
                ));
        }
        
        await app.RunAsync();
    }
}
