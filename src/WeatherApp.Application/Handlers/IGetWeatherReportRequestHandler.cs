using WeatherApp.Application.Models;
using WeatherApp.Domain.Outcomes;

namespace WeatherApp.Application.Handlers;

public interface IGetWeatherReportRequestHandler
{
    Task<OneOf<WeatherReportResponse, Failure>> HandleGetWeatherReport(string requestedRegion, DateTime requestedDate);
    // OneOf gives us discriminated unions in C#!😁
}