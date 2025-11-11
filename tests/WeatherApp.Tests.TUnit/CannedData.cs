using Microsoft.Identity.Client;
using WeatherApp.Application.Models.Requests;
using WeatherApp.Domain.DomainEvents;
using WeatherApp.Domain.EventSourcing;
using WeatherApp.Domain.ValueObjects;

namespace WeatherApp.Tests.TUnit;

public class CannedData
{
    private readonly Random Random = new();

    public Guid StreamId {get; set;}
    public string Reference {get; set;}
    public string Location {get; set;}
    public Guid RequestId {get; set;}

    public string modelingDataRejectedReason { get; set; }

    public CannedData(Guid? streamId = null, string? location = null, string? reference = null, Guid? requestId = null)
    {        
        StreamId = streamId ?? Guid.NewGuid();
        Location = location ?? $"testLocation{Guid.NewGuid()}"[..20];
        Reference = reference ?? $"testRef{Guid.NewGuid()}"[..10];
        RequestId = requestId ?? Guid.NewGuid();

        modelingDataRejectedReason = $"Reason{Random.Next(10, 99)}";      
    }

    public decimal GetRandomWindSpeed() => (decimal)(Random.Next(1, 69) + Random.NextSingle());
    public string GetRandomWindDirection() => new List<string> { "N", "NE", "E", "SE", "S", "SW", "W", "NW" }[Random.Next(0, 7)];
    public decimal GetRandomTemperature() => (decimal)(Random.Next(-15, 45) + Random.NextSingle());
    public decimal GetRandomHumidity() => (decimal)Random.NextSingle();
    
    public CollectedWeatherDataPointModel GetRandomCollectedWeatherDataPointModel() =>
        new (
            DateTimeOffset.UtcNow, 
            GetRandomWindSpeed(),
            GetRandomWindDirection(),
            GetRandomTemperature(),
            GetRandomHumidity());

    public CollectedWeatherDataModel GetRandCollectedWeatherDataModel(int count) => 
        new (Enumerable.Range(0, count).Select(_ => GetRandomCollectedWeatherDataPointModel()).ToList());

    public PendingContributorPayment GetRandomPendingContributorPayment() => 
        new (Guid.NewGuid(), Guid.NewGuid(), Random.Next(10, 20), "GBP", $"Weather data collection payment for requestId: {RequestId}");

    // Domain events
    public WeatherDataCollectionInitiated WeatherDataCollectionInitiated(string? location = null, string? reference = null, string? idempotencyKey = null)
    {
        return new WeatherDataCollectionInitiated(
            GetRandCollectedWeatherDataModel(3).ToEntity(), 
            location ?? Location, 
            reference ?? Reference, 
            idempotencyKey ?? RequestId.ToString());
    }

    public LocationIdFound LocationIdFound() => 
        new (Guid.NewGuid());

    public PendingContributorPaymentPosted PendingContributorPaymentPosted() => 
        new PendingContributorPaymentPosted(GetRandomPendingContributorPayment());

    public ModelingDataRejected ModellingDataRejected(string? reason = null) => 
        new ModelingDataRejected(reason ?? modelingDataRejectedReason);

    public PendingContributorPaymentRevoked PendingContributorPaymentRevoked(Guid? paymentId = null) => 
        new PendingContributorPaymentRevoked(paymentId ?? Guid.NewGuid());    

    // Scenario domain event collections
    public List<Event> UpTo_WeatherDataCollectionInitiated(string? location = null, string? reference = null, string? idempotencyKey = null)
    {        
        var version = 1;
        return new List<Event> {
            Event.Create(WeatherDataCollectionInitiated(location, reference, idempotencyKey), StreamId, version++, null)
        };
    }

    public List<Event> UpTo_WeatherModelingServiceRejectionFailure()
    {        
        var version = 1;
        var streamId = Guid.NewGuid();
        return new List<Event> {
            Event.Create(WeatherDataCollectionInitiated(), streamId, version++, null),
            Event.Create(LocationIdFound(), streamId, version++, null),
            Event.Create(PendingContributorPaymentPosted(), streamId, version++, null),
            Event.Create(ModellingDataRejected(), streamId, version++, null),
            Event.Create(PendingContributorPaymentRevoked(), streamId, version++, null),
        };
    }
}
