using WeatherApp.Domain.Entities;
using WeatherApp.Domain.EventSourcing;
using WeatherApp.Domain.ValueObjects;

namespace WeatherApp.Domain.DomainEvents;

public record WeatherDataCollectionInitiated(CollectedWeatherData Data, string Location, string reference) : IDomainEvent;
public record LocationIdFound(Guid LocationId) : IDomainEvent;
public record SubmittedToModeling(Guid SubmissionId) : IDomainEvent;
public record ModelingDataAccepted() : IDomainEvent;
public record SubmissionComplete() : IDomainEvent;
public record ModelingDataRejected(string Reason) : IDomainEvent;
public record ModelUpdated() : IDomainEvent;
public record PendingContributorPaymentPosted(PendingContributorPayment PendingContributorPayment) : IDomainEvent;
public record PendingContributorPaymentRevoked(Guid PaymentId) : IDomainEvent;
public record PendingContributorPaymentCommitted(Guid PaymentId) : IDomainEvent;