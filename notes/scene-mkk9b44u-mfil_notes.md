# Notes for slides

- Mock ContributorPaymentsService and WeatherModelingService APIs
- The test will send integration events mimicing the WeatherModelingService
- Database repositories are in-memory lists
- No NotificationService, we just assert that outbox item is dispatched 
