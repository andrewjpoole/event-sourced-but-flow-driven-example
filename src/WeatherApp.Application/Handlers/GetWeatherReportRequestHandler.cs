using WeatherApp.Application.Models;
using WeatherApp.Application.Services;
using WeatherApp.Domain;
using WeatherApp.Domain.Outcomes;

namespace WeatherApp.Application.Handlers;

public class GetWeatherReportRequestHandler(
    IRegionValidator regionValidator,
    IDateChecker dateChecker,
    IWeatherForecastGenerator weatherForecastGenerator)
    : IGetWeatherReportRequestHandler
{
    public async Task<OneOf<WeatherReportResponse, Failure>> HandleGetWeatherReport(
        string requestedRegion, DateTime requestedDate)
    {
        return await WeatherReportDetails.Create(requestedRegion, requestedDate)
            .Then(regionValidator.ValidateRegion)
            .Then(dateChecker.CheckDate)
            .Then(CheckCache)
            .IfThen(d => d.PopulatedFromCache is false, 
                weatherForecastGenerator.Generate)
            .ToResult(WeatherReportResponse.FromDetails);
    }

    public async Task<OneOf<WeatherReportDetails, Failure>> CheckCache(
        WeatherReportDetails details)
    {
        // Check and populate from a local in-memory cache etc...
        // Methods from anywhere can be chained as long as they
        // have the correct return type, matching the T and TFailure for the chain...
        
        await Task.Delay(50);
        details.Set("summary from cache", 32);
        details.PopulatedFromCache = true;
        
        return details;
    }
}