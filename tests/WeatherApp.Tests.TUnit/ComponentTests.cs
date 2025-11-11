using System.Net;
using WeatherApp.Application.Models;
using Shouldly;
using WeatherApp.Application.Models.IntegrationEvents.NotificationEvents;
using WeatherApp.Application.Models.IntegrationEvents.WeatherModelingEvents;
using WeatherApp.Domain.DomainEvents;

namespace WeatherApp.Tests.TUnit;

public class ComponentTests
{
    private ComponentTestFixture testFixture;

    [Before(Test)]
    public void Setup()
    {
        testFixture = new ComponentTestFixture();
    }

    [Test]
    public void Return_a_WeatherReport_given_valid_region_and_date()
    {
        var (given, when, then, cannedData) = testFixture.SetupHelpers();

        given.WeHaveAWeatherReportRequest("bristol", DateTime.Now, out var apiRequest)
            .And.TheServersAreStarted();

        when.WeSendTheMessageToTheApi(apiRequest, out var response);

        then.TheResponseCodeShouldBe(response, HttpStatusCode.OK)
            .And.TheBodyShouldNotBeEmpty<WeatherReportResponse>(response, 
                x => x.Summary.ShouldNotBeEmpty());
    }

    [Test]
    public void e2e_flow_notifications_sent_when_ModelingDataAccepted()
    {
        var (given, when, then, cannedData) = testFixture.SetupHelpers();
        given.WeHaveSomeCollectedWeatherData(cannedData, out var weatherData)
            .And.TheContributorPaymentsServiceCreateEndpointWillReturn(HttpStatusCode.Accepted)
            .And.TheModelingServiceSubmitEndpointWillReturn(HttpStatusCode.Accepted)
            .And.TheServersAreStarted();
        
        when.InPhase("1 (initial API request)") 
            .And.WeWrapTheCollectedWeatherDataInAnHttpRequestMessage(weatherData, cannedData, out var httpRequest)
            .And.WeSendTheMessageToTheApi(httpRequest, out var response);

        then.And.TheModelingServiceSubmitEndpointShouldHaveBeenCalled(times: 1)
            .And.TheDomainEventShouldHaveBeenPersisted<SubmittedToModeling>()
            .And.TheResponseCodeShouldBe(response, HttpStatusCode.OK)
            .And.TheBodyShouldNotBeEmpty(response)
            .And.WeGetTheStreamIdFromTheInitialDomainEvent(cannedData.RequestId, out var streamId);
        
        when.InPhase("2 (1st ASB message back from modeling service)")
            .AMessageAppears(new ModelingDataAcceptedIntegrationEvent(streamId));

        then.TheDomainEventShouldHaveBeenPersisted<ModelingDataAccepted>();

        when.InPhase("3 (2nd ASB message back from modeling service)")
            .AMessageAppears(new ModelUpdatedIntegrationEvent(streamId));

        then.TheDomainEventShouldHaveBeenPersisted<ModelUpdated>()
            .And.AnOutboxRecordWasInserted<UserNotificationEvent>();

        then.InPhase("4 (Notification Service handles event dispached by outbox)")
            .AfterSomeTimeHasPassed(1_000, 500)
            .And.TheMessageWasHandled<ModelUpdatedIntegrationEvent>()
            .And.TheNotificationServiceNotifiedTheUser(cannedData.Location, cannedData.Reference);
    }

    [Test]
    public void ApiPhase_EndsInConflict_IfExistingAggregateFound_WithSameRequestIdButDifferentReference()
    {
        var (given, when, then, cannedData) = testFixture.SetupHelpers();
        given.WeHaveSomeCollectedWeatherData(cannedData, out var weatherData)
            .And.WeHaveResetEverything()
            .And.ThereIsExistingData(cannedData.UpTo_WeatherDataCollectionInitiated(reference: "differentReference"))
            .And.TheServersAreStarted();
        
        when.InPhase("1 (initial API request)") 
            .And.WeWrapTheCollectedWeatherDataInAnHttpRequestMessage(weatherData, cannedData, out var httpRequest)
            .And.WeSendTheMessageToTheApi(httpRequest, out var response);

        then.And.TheModelingServiceSubmitEndpointShouldNotHaveBeenCalled()
            .And.TheResponseCodeShouldBe(response, HttpStatusCode.Conflict);
    }

    [Test]
    public void ApiPhase_EndsInSameFailure_IfExistingAggregateFound_WhichEndedInAPermanentFailure()
    {
        var (given, when, then, cannedData) = testFixture.SetupHelpers();
        given.WeHaveSomeCollectedWeatherData(cannedData, out var weatherData)
            .And.WeHaveResetEverything()
            .And.ThereIsExistingData(cannedData.UpTo_WeatherModelingServiceRejectionFailure())
            .And.TheServersAreStarted();
        
        when.InPhase("1 (initial API request)") 
            .And.WeWrapTheCollectedWeatherDataInAnHttpRequestMessage(weatherData, cannedData, out var httpRequest)
            .And.WeSendTheMessageToTheApi(httpRequest, out var response);

        then.And.TheModelingServiceSubmitEndpointShouldNotHaveBeenCalled()
            .And.TheResponseCodeShouldBe(response, HttpStatusCode.UnprocessableEntity)
            .And.TheBodyShouldNotBeEmpty<string>(response, 
                x => x.ShouldBe(cannedData.modelingDataRejectedReason));
    }

    [After(Test)]
    public void TearDown()
    {
        testFixture.Dispose();
    }
}