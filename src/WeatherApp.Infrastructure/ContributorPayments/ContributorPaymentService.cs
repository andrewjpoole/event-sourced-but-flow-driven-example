﻿using Microsoft.Extensions.Logging;
using WeatherApp.Application.Services;
using WeatherApp.Domain.Entities;
using WeatherApp.Domain.Outcomes;
using WeatherApp.Domain.ValueObjects;
using WeatherApp.Domain.Logging;
using WeatherApp.Infrastructure.ApiClientWrapper;

namespace WeatherApp.Infrastructure.ContributorPayments;

public class ContributorPaymentService(
    IRefitClientWrapper<IContributorPaymentServiceClient> clientWrapper,
    ILogger<ContributorPaymentService> logger) : IContributorPaymentService
{
    private Guid contributorId = Guid.NewGuid(); // Canned data for testing

    public async Task<OneOf<WeatherDataCollectionAggregate, Failure>> CreatePendingPayment(WeatherDataCollectionAggregate weatherDataCollectionAggregate)
    {
        if(weatherDataCollectionAggregate.PendingPayment != null)
        {
            logger.LogWarning("Skipping payment creation as a pending payment already exists for streamId: {StreamId}", weatherDataCollectionAggregate.StreamId);
            return weatherDataCollectionAggregate;
        }
        
        using var client = clientWrapper.CreateClient();

        var paymentId = Guid.NewGuid();
        var pendingPayment = new PendingContributorPayment
        (
            contributorId,
            paymentId,
            Random.Shared.Next(10, 20),
            "GBP",
            $"Weather data collection payment for requestId: {weatherDataCollectionAggregate.StreamId}"
        );

        HttpResponseMessage? response = null;
        try
        {
            response = await client.PostPendingPayment(contributorId, pendingPayment);

        if(response.IsSuccessStatusCode != true)
        {
            var bodyContent = await response.Content.ReadAsStringAsync();
            logger.LogFailedContributerPaymentRequest("post pending", weatherDataCollectionAggregate.StreamId, pendingPayment);
            return OneOf<WeatherDataCollectionAggregate, Failure>.FromT1(new ContributorPaymentServiceFailure(bodyContent));
        }
        }
        catch (System.Exception ex)
        {
            logger.LogError(ex, "Error creating pending payment for streamId: {StreamId}", weatherDataCollectionAggregate.StreamId);
            throw;
        }
        

        await weatherDataCollectionAggregate.AppendPendingContributorPaymentEvent(pendingPayment);

        return OneOf<WeatherDataCollectionAggregate, Failure>.FromT0(weatherDataCollectionAggregate);
    }

    public async Task<OneOf<WeatherDataCollectionAggregate, Failure>> RevokePendingPayment(WeatherDataCollectionAggregate weatherDataCollectionAggregate)
    {
        if(weatherDataCollectionAggregate.PendingPaymentRevoked)
        {
            logger.LogWarning("Skipping payment revocation as a pending payment has already been revoked for streamId: {StreamId}", weatherDataCollectionAggregate.StreamId);
            return weatherDataCollectionAggregate;
        }

        using var client = clientWrapper.CreateClient();

        var pendingPayment = weatherDataCollectionAggregate.PendingPayment ?? 
            throw new InvalidOperationException("No pending payment to revoke.");

        var response = await client.RevokePayment(contributorId, pendingPayment.PaymentId);

        if(response.IsSuccessStatusCode != true)
        {
            var bodyContent = response.Content.ReadAsStringAsync().Result;
            logger.LogFailedContributerPaymentRequest("revoke", weatherDataCollectionAggregate.StreamId, pendingPayment);
            return OneOf<WeatherDataCollectionAggregate, Failure>.FromT1(new ContributorPaymentServiceFailure(bodyContent));
        }

        await weatherDataCollectionAggregate.AppendRevokedContributorPaymentEvent(pendingPayment.PaymentId);

        return OneOf<WeatherDataCollectionAggregate, Failure>.FromT0(weatherDataCollectionAggregate);
    }

    public async Task<OneOf<WeatherDataCollectionAggregate, Failure>> CommitPendingPayment(WeatherDataCollectionAggregate weatherDataCollectionAggregate)
    {
        using var client = clientWrapper.CreateClient();

        var pendingPayment = weatherDataCollectionAggregate.PendingPayment ?? 
            throw new InvalidOperationException("No pending payment to commit.");

        var response = await client.CommitPayment(contributorId, pendingPayment.PaymentId);

        if(response.IsSuccessStatusCode != true)
        {
            var bodyContent = response.Content.ReadAsStringAsync().Result;
            logger.LogFailedContributerPaymentRequest("commit", weatherDataCollectionAggregate.StreamId, pendingPayment);
            return OneOf<WeatherDataCollectionAggregate, Failure>.FromT1(new ContributorPaymentServiceFailure(bodyContent));
        }

        await weatherDataCollectionAggregate.AppendCommittedContributorPaymentEvent(pendingPayment.PaymentId);

        return OneOf<WeatherDataCollectionAggregate, Failure>.FromT0(weatherDataCollectionAggregate);
    }
}