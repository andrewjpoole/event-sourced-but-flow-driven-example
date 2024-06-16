namespace WeatherApp.Application.Models.IntegrationEvents.WeatherModelingEvents;

public record ModelingDataAcceptedIntegrationEvent(Guid RequestId) : ModelingIntegrationEvent(RequestId);