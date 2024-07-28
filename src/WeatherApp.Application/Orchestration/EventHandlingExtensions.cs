using WeatherApp.Domain.Entities;
using WeatherApp.Domain.Outcomes;

namespace WeatherApp.Application.Orchestration;

public static class EventHandlingExtensions
{
    public static async Task ThrowOnFailure(this Task<OneOf<WeatherDataCollectionAggregate, Failure>> successOrFailure, string eventName)
    {
        var result = await successOrFailure;
        if (result.IsT1)
            throw new Exception($"Something went wrong while handling {eventName}");
    }
}