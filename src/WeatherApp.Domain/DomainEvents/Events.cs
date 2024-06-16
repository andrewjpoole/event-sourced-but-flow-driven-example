using WeatherApp.Domain.Entities;
using WeatherApp.Domain.EventSourcing;

namespace WeatherApp.Domain.DomainEvents;

public record WeatherDataCollectionInitiated(CollectedWeatherData Data, string Location) : IDomainEvent;
public record LocationIdFound(Guid LocationId) : IDomainEvent;
public record SubmittedToModeling(Guid SubmissionId) : IDomainEvent;
public record ModelingDataAccepted() : IDomainEvent;
public record SubmissionComplete() : IDomainEvent;
public record ModelingDataRejected(string Reason) : IDomainEvent;
public record ModelUpdated() : IDomainEvent;