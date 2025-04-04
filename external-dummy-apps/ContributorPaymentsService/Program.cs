using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using WeatherApp.Domain.ValueObjects;

namespace ContributorPaymentsService;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Logging
            .ClearProviders()
            .AddConsole();

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(builder.Environment.ApplicationName))
            .WithLogging(logging => logging
                .AddOtlpExporter());
        
        var app = builder.Build();

        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        
        app.UseHttpsRedirection();
        
        app.MapGet("/", () => "Contributor Payments Service is running!");

        app.MapPost("/v1/contributor-payments/{contributorId}/pending", (Guid contributorId, [FromBody] PendingContributorPayment payment) =>
        {
            logger.LogInformation("Received pending payment request for Contributor ID: {ContributorId} PaymentId: {PaymentId} Amount: {Amount}", contributorId, payment.PaymentId, payment.Amount);
            return Results.Ok(new { ContributorId = contributorId, PaymentId = payment.PaymentId, Status = "Pending" });
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