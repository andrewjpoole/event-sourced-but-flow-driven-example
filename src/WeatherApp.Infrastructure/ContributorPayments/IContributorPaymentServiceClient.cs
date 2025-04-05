using Refit;
using WeatherApp.Domain.ValueObjects;

namespace WeatherApp.Infrastructure.ContributorPayments;

public interface IContributorPaymentServiceClient : IDisposable
{
    [Post("/v1/contributor-payments/{contributorId}/pending")]
    Task<HttpResponseMessage> PostPendingPayment(Guid contributorId, [Body] PendingContributorPayment payment);

    [Post("/v1/contributor-payments/{contributorId}/commit/{paymentId}")]
    Task<HttpResponseMessage> CommitPayment(Guid contributorId, Guid paymentId);

    [Post("/v1/contributor-payments/{contributorId}/revoke/{paymentId}")]
    Task<HttpResponseMessage> RevokePayment(Guid contributorId, Guid paymentId);
}