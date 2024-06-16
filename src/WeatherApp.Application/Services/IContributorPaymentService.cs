using WeatherApp.Domain.Entities;
using WeatherApp.Domain.Outcomes;

namespace WeatherApp.Application.Services;

public interface IContributorPaymentService
{
    Task<OneOf<WeatherDataCollection, Failure>> CreatePendingPayment(
        WeatherDataCollection weatherDataCollection);
    Task<OneOf<WeatherDataCollection, Failure>> RevokePendingPayment(
        WeatherDataCollection weatherDataCollection);
    Task<OneOf<WeatherDataCollection, Failure>> CommitPendingPayment(
        WeatherDataCollection weatherDataCollection);
}