using Microsoft.AspNetCore.Mvc;

using WeatherApp.Domain.ValueObjects;

namespace ContributorPaymentsService;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();
        
        var app = builder.Build();

        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        
        app.UseHttpsRedirection();
        
        app.MapGet("/", () => "Contributor Payments Service is running!");

        app.MapPost("/v1/contributor-payments/{contributorId}/pending", (Guid contributorId, [FromBody] PendingContributorPayment payment) =>
        {
            logger.LogInformation("Received pending payment request for Contributor ID: {ContributorId} PaymentId: {PaymentId} Amount: {Amount}", contributorId, payment.PaymentId, payment.Amount);
            return Results.Ok(new { ContributorId = contributorId, payment.PaymentId, Status = "Pending" });
        });

        app.MapPost("/v1/contributor-payments/{contributorId}/revoke/{paymentId}", (Guid contributorId, Guid paymentId) =>
        {
            logger.LogInformation("Revoking payment for Contributor ID: {ContributorId} PaymentId: {PaymentId}", contributorId, paymentId);
            return Results.Ok(new { ContributorId = contributorId, PaymentId = paymentId, Status = "Revoked" });
        });

        app.MapPost("/v1/contributor-payments/{contributorId}/commit/{paymentId}", (Guid contributorId, Guid paymentId) =>
        {
            logger.LogInformation("Committing payment for Contributor ID: {ContributorId} PaymentId: {PaymentId}", contributorId, paymentId);
            return Results.Ok(new { ContributorId = contributorId, PaymentId = paymentId, Status = "Committed" });
        });

        await app.RunAsync();
    }
}