﻿using Microsoft.Extensions.Logging;
using WeatherApp.Application.Services;
using WeatherApp.Domain.DomainEvents;
using WeatherApp.Domain.Entities;
using WeatherApp.Domain.Logging;
using WeatherApp.Domain.Outcomes;
using WeatherApp.Infrastructure.ApiClients.WeatherModelingSystem;
using WeatherApp.Infrastructure.ApiClientWrapper;

namespace WeatherApp.Infrastructure.WeatherModelingSystem;

public class WeatherModelingService(
    ILogger<WeatherModelingService> logger,
    IRefitClientWrapper<IWeatherModelingServiceClient> weatherModelingServiceClientWrapper    
    ) : IWeatherModelingService
{
    public async Task<OneOf<WeatherDataCollectionAggregate, Failure>> Submit(WeatherDataCollectionAggregate weatherDataCollectionAggregate)
    {
        // calls out to an external service which returns an Accepted response
        // the result will be communicated via a service bus message...

        // If previously failed, just return the failure
        if(weatherDataCollectionAggregate.ModelingDataRejected)
        {
            var reason = weatherDataCollectionAggregate.ModelingDataRejectedReason!;
            return OneOf<WeatherDataCollectionAggregate, Failure>.FromT1(new WeatherModelingServiceRejectionFailure(reason));
        }

        using var weatherModelingServiceClient = weatherModelingServiceClientWrapper.CreateClient();

        var response = await weatherModelingServiceClient.PostCollectedData(
            weatherDataCollectionAggregate.Location, 
            weatherDataCollectionAggregate.StreamId, 
            weatherDataCollectionAggregate.Data);
            
        var bodyContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode) 
            return OneOf<WeatherDataCollectionAggregate, Failure>.FromT1(new WeatherModelingServiceRejectionFailure(bodyContent));
        
        var submissionId = Guid.Parse(bodyContent);
        await weatherDataCollectionAggregate.AppendEvent(new SubmittedToModeling(submissionId));

        logger.LogWeatherDataSubmittedToModelingService(weatherDataCollectionAggregate.Location, submissionId);

        return weatherDataCollectionAggregate;
    }
}