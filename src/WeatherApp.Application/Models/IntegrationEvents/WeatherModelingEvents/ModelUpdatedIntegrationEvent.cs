namespace WeatherApp.Application.Models.IntegrationEvents.WeatherModelingEvents;

public record ModelUpdatedIntegrationEvent(Guid RequestId) : ModelingIntegrationEvent(RequestId);