using Microsoft.AspNetCore.Mvc;
using WeatherApp.Domain.Outcomes;

namespace WeatherApp.WebAPI;

public static class FailureExtensions
{
    public static ProblemDetails ToProblemDetails(this UnsupportedRegionFailure failure)
    {
        return new ProblemDetails
        {
            Type = "some origin...",
            Title = failure.Title,
            Detail = failure.Detail
        };
    }

    public static ProblemDetails ToValidationProblemDetails(this InvalidRequestFailure failure)
    {
        return new ValidationProblemDetails(failure.ValidationErrors);
    }
}