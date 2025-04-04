namespace WeatherApp.Application.Models.IntegrationEvents.WeatherModelingEvents;

public record ModelingDataRejectedIntegrationEvent(Guid StreamId, string Reason) : ModelingIntegrationEvent(StreamId);