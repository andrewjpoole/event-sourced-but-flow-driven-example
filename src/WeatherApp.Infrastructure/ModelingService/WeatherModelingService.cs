using WeatherApp.Application.Services;
using WeatherApp.Domain.DomainEvents;
using WeatherApp.Domain.Entities;
using WeatherApp.Domain.Outcomes;
using WeatherApp.Infrastructure.ApiClients.WeatherModelingSystem;

namespace WeatherApp.Infrastructure.ModelingService;

public class WeatherModelingService(IRefitClientWrapper<IWeatherModelingServiceClient> weatherModelingServiceClientWrapper) : IWeatherModelingService
{
    public async Task<OneOf<WeatherDataCollectionAggregate, Failure>> Submit(WeatherDataCollectionAggregate weatherDataCollectionAggregate)
    {
        // calls out to an external service which returns an Accepted response
        // the result will be communicated via a service bus message...

        using var weatherModelingServiceClient = weatherModelingServiceClientWrapper.CreateClient();

        var response = await weatherModelingServiceClient.PostCollectedData(weatherDataCollectionAggregate.Location, weatherDataCollectionAggregate); // response is null in LoggingHttpMessageHandler.Log.RequestEnd ???
        var bodyContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode) 
            return OneOf<WeatherDataCollectionAggregate, Failure>.FromT1(new WeatherModelingServiceRejectionFailure(bodyContent));
        
        var submissionId = Guid.Parse(bodyContent);
        await weatherDataCollectionAggregate.AppendEvent(new SubmittedToModeling(submissionId));

        return weatherDataCollectionAggregate;
    }
}