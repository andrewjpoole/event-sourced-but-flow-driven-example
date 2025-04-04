namespace WeatherApp.Application.Models.IntegrationEvents.WeatherModelingEvents;

public record ModelUpdatedIntegrationEvent(Guid StreamId) : ModelingIntegrationEvent(StreamId);