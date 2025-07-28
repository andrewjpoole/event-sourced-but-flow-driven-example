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
    /*
    // Example of what we didn't want...
    public async Task<OneOf<WeatherReport, Failure>> HandleGetWeatherReport(
        string requestedRegion, DateTime requestedDate)
    {
        var isValidRequest = await regionValidator.Validate(requestedRegion);
        if (!isValidRequest)
            return new UnsupportedRegionFailure();

        var dateCheckPassed = await dateChecker.CheckDate(requestedDate);
        if (!dateCheckPassed)
            return new InvalidRequestFailure();

        var cacheCheckResult = CheckCache(requestedRegion, requestedDate);
        if (cacheCheckResult.Hit)
            return cacheCheckResult.Data;
        else
            return weatherForecastGenerator.Generate(requestedRegion, requestedDate);
    }



























    */

    public async Task<OneOf<WeatherReportResponse, Failure>> HandleGetWeatherReport(
        string requestedRegion, DateTime requestedDate)
    {
        var settings = "34jh5k4jh5";
        return await WeatherReportDetails.Create(requestedRegion, requestedDate)
            .Then(regionValidator.ValidateRegion)
            .Then(dateChecker.CheckDate)
            .Then(d => CheckCache(d, settings))
            .IfThen(d => d.PopulatedFromCache == false,
                weatherForecastGenerator.Generate)
            .ToResult(WeatherReportResponse.FromDetails);
    }

    /*
    
    
    
    
    
    
    
    
    
    
    
    
    */

    public async Task<OneOf<WeatherReportDetails, Failure>> CheckCache(
        WeatherReportDetails details, string settings)
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