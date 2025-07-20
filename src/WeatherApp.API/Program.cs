using Microsoft.AspNetCore.Mvc;
using WeatherApp.Application.Handlers;
using WeatherApp.Application.Models.Requests;
using WeatherApp.Application.Orchestration;
using WeatherApp.Application.Services;
using WeatherApp.Domain.Outcomes;
using WeatherApp.Infrastructure.ApiClients.WeatherModelingSystem;
using WeatherApp.Infrastructure.ContributorPayments;
using WeatherApp.Infrastructure.LocationManager;
using WeatherApp.Infrastructure.Persistence;
using WeatherApp.Infrastructure.ApiClientWrapper;
using WeatherApp.Infrastructure.Outbox;

namespace WeatherApp.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();
        builder.AddSqlServerClient(connectionName: "WeatherAppDb");
           
        // Add services to the container
        builder.Services
            .AddDatabaseConnectionFactory()
            .AddOutboxServices()
            .AddEventSourcing()
            .AddSingleton<IGetWeatherReportRequestHandler, GetWeatherReportRequestHandler>()
            .AddSingleton<ISubmitWeatherDataCommandHandler, CollectedWeatherDataOrchestrator>()
            .AddSingleton<IRegionValidator, RegionValidator>()
            .AddSingleton<IDateChecker, DateChecker>()
            .AddSingleton<IWeatherForecastGenerator, WeatherForecastGenerator>()
            .AddContributorPaymentsService(builder.Configuration)
            .AddSingleton<IWeatherDataValidator, WeatherDataValidator>()
            .AddSingleton<ILocationManager, LocationManager>()
            .AddSingleton<IContributorPaymentService, ContributorPaymentService>()
            .AddWeatherModelingService(builder.Configuration)
            .AddSingleton(typeof(IRefitClientWrapper<>), typeof(RefitClientWrapper<>));

        var app = builder.Build();

        app.UseHttpsRedirection();

        app.MapGet("/", () => "WeatherApp API is running!");

        app.MapGet("/v1/weather-forecast/{region}/{date}", (
            [FromRoute] string region,
            [FromRoute] DateTime date,
            [FromServices] IGetWeatherReportRequestHandler handler)
            => CreateResponseFor(() => handler.HandleGetWeatherReport(region, date)));

        #region
        app.MapPost("/v1/collected-weather-data/{location}/{reference}", (
            [FromRoute] string location,
            [FromRoute] string reference,
            [FromHeader(Name = "x-request-id")] Guid requestId,
            [FromBody] CollectedWeatherDataModel data,
            [FromServices] ISubmitWeatherDataCommandHandler handler,
            [FromServices] IWeatherDataValidator weatherDataValidator,
            [FromServices] ILocationManager locationManager)
            => CreateResponseFor(() => handler.HandleSubmitWeatherDataCommand(location, reference, requestId, data, weatherDataValidator, locationManager)));
        #endregion

        static async Task<IResult> CreateResponseFor<TSuccess>(Func<Task<OneOf<TSuccess, Failure>>> handleRequestFunc)
        {
            var result = await handleRequestFunc.Invoke();
            return result.Match(
                success => Results.Ok(success),
                failure => failure.Match(
                    invalidRequestFailure => Results.BadRequest(invalidRequestFailure.ToValidationProblemDetails()),
                    unsupportedRegionFailure => Results.UnprocessableEntity(unsupportedRegionFailure.ToProblemDetails()),
                    modelingServiceRejectionFailure => Results.UnprocessableEntity(modelingServiceRejectionFailure.Message),
                    contributorPaymentServiceFailure => Results.UnprocessableEntity(contributorPaymentServiceFailure.Message),
                    alreadyProcessedFailure => Results.Conflict(alreadyProcessedFailure.Message))
                );
        }
        
        await app.RunAsync();
    }
}
