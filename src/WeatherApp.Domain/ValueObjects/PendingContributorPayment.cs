namespace WeatherApp.Domain.ValueObjects;

public record PendingContributorPayment
{
    public Guid ContributorId { get; }
    public Guid PaymentId { get; }
    public decimal Amount { get; }
    public string Currency { get; }
    public string Description { get; }

    public PendingContributorPayment(Guid contributorId, Guid paymentId, decimal amount, string currency, string description)
    {
        // todo add rules here...

        ContributorId = contributorId;
        PaymentId = paymentId;
        Amount = amount;
        Currency = currency;
        Description = description;
    }
}