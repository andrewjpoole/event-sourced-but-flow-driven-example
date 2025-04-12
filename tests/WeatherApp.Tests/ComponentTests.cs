using System.Net;
using FluentAssertions;
using WeatherApp.Application.Models;
using WeatherApp.Application.Models.IntegrationEvents.WeatherModelingEvents;
using WeatherApp.Application.Models.Requests;
using WeatherApp.Domain.DomainEvents;

namespace WeatherApp.Tests;

public class ComponentTests(ComponentTestFixture testFixture) : IClassFixture<ComponentTestFixture>
{
    [Fact]
    public void Return_a_WeatherReport_given_valid_region_and_date()
    {
        var (given, when, then) = testFixture.SetupHelpers();

        given.WeHaveAWeatherReportRequest("bristol", DateTime.Now, out var apiRequest)
            .And.TheServersAreStarted();

        when.WeSendTheMessageToTheApi(apiRequest, out var response);

        then.TheResponseCodeShouldBe(response, HttpStatusCode.OK)
            .And.TheBodyShouldNotBeEmpty<WeatherReportResponse>(response, 
                x => x.Summary.Should().NotBeEmpty());
    }

    [Fact]
    public void e2e_flow_notifications_sent_when_ModelingDataAccepted()
    {
        var (given, when, then) = testFixture.SetupHelpers();

        var testLocation = $"testLocation{Guid.NewGuid()}"[..20];
        var testReference = $"testReference{Guid.NewGuid()}"[..5];

        given.WeHaveSomeCollectedWeatherData(out var weatherData)
            .And.TheContributorPaymentsServicePendingEndpointWillReturn(HttpStatusCode.Accepted)
            .And.TheModelingServiceSubmitEndpointWillReturn(HttpStatusCode.Accepted)
            .And.TheServersAreStarted();
        
        when.InPhase("1 (initial API request)") 
            .And.WeWrapTheCollectedWeatherDataInAnHttpRequestMessage(weatherData, testLocation, testReference, out var httpRequest)
            .And.WeSendTheMessageToTheApi(httpRequest, out var response);

        then.And.TheModelingServiceSubmitEndpointShouldHaveBeenCalled(times: 1)
            .And.TheEventShouldHaveBeenPersisted<SubmittedToModeling>()
            .And.TheResponseCodeShouldBe(response, HttpStatusCode.OK)
            .And.TheBodyShouldNotBeEmpty<WeatherDataCollectionResponse>(response, out var responseBody);
        
        when.InPhase("2 (1st ASB message back from modeling service)")
            .AMessageAppears(message: new ModelingDataAcceptedIntegrationEvent(responseBody.RequestId));

        then.TheEventShouldHaveBeenPersisted<ModelingDataAccepted>();

        when.InPhase("3 (2nd ASB message back from modeling service)")
            .AMessageAppears(message: new ModelUpdatedIntegrationEvent(responseBody.RequestId));

        then.TheEventShouldHaveBeenPersisted<ModelUpdated>()
            .And.AnOutboxRecordWasInserted();

        then.InPhase("4 (Notification Service handles event dispached by outbox)")
            .AfterSomeTimeHasPassed(5_000, 2_000)
            .And.TheMessageWasHandled<ModelUpdatedIntegrationEvent>()
            .And.TheNotificationServiceNotifiedTheUser(testLocation, testReference);
    }
}