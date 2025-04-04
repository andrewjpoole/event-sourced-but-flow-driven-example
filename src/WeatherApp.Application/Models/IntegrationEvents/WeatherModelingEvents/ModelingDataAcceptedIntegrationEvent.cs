namespace WeatherApp.Application.Models.IntegrationEvents.WeatherModelingEvents;

public record ModelingDataAcceptedIntegrationEvent(Guid StreamId) : ModelingIntegrationEvent(StreamId);