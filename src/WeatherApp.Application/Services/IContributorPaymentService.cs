using WeatherApp.Domain.Entities;
using WeatherApp.Domain.Outcomes;

namespace WeatherApp.Application.Services;

public interface IContributorPaymentService
{
    Task<OneOf<WeatherDataCollectionAggregate, Failure>> CreatePendingPayment(
        WeatherDataCollectionAggregate weatherDataCollectionAggregate);
    Task<OneOf<WeatherDataCollectionAggregate, Failure>> RevokePendingPayment(
        WeatherDataCollectionAggregate weatherDataCollectionAggregate);
    Task<OneOf<WeatherDataCollectionAggregate, Failure>> CommitPendingPayment(
        WeatherDataCollectionAggregate weatherDataCollectionAggregate);
}