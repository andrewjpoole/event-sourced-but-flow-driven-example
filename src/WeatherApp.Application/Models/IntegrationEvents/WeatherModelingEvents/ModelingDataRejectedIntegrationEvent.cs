namespace WeatherApp.Application.Models.IntegrationEvents.WeatherModelingEvents;

public record ModelingDataRejectedIntegrationEvent(Guid RequestId, string Reason) : ModelingIntegrationEvent(RequestId);