using WeatherApp.Application.Services;
using WeatherApp.Domain.Entities;
using WeatherApp.Domain.Outcomes;

namespace WeatherApp.Infrastructure.ContributorPayments;

public class ContributorPaymentService : IContributorPaymentService
{
    public Task<OneOf<WeatherDataCollectionAggregate, Failure>> CreatePendingPayment(WeatherDataCollectionAggregate weatherDataCollectionAggregate)
    {
        // todo: add a refit client?
        return Task.FromResult(OneOf<WeatherDataCollectionAggregate, Failure>.FromT0(weatherDataCollectionAggregate));
    }

    public Task<OneOf<WeatherDataCollectionAggregate, Failure>> RevokePendingPayment(WeatherDataCollectionAggregate weatherDataCollectionAggregate)
    {
        return Task.FromResult(OneOf<WeatherDataCollectionAggregate, Failure>.FromT0(weatherDataCollectionAggregate));
    }

    public Task<OneOf<WeatherDataCollectionAggregate, Failure>> CommitPendingPayment(WeatherDataCollectionAggregate weatherDataCollectionAggregate)
    {
        return Task.FromResult(OneOf<WeatherDataCollectionAggregate, Failure>.FromT0(weatherDataCollectionAggregate));
    }
}