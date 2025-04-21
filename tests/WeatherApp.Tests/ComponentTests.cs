using System.Net;
using NUnit.Framework;
using WeatherApp.Application.Models;
using WeatherApp.Application.Models.IntegrationEvents.NotificationEvents;
using WeatherApp.Application.Models.IntegrationEvents.WeatherModelingEvents;
using WeatherApp.Application.Models.Requests;
using WeatherApp.Domain.DomainEvents;

namespace WeatherApp.Tests;

[TestFixture]
public class ComponentTests
{
    private ComponentTestFixture testFixture;

    [SetUp]
    public void Setup()
    {
        testFixture = new ComponentTestFixture();
    }

    [Test]
    public void Return_a_WeatherReport_given_valid_region_and_date()
    {
        var (given, when, then) = testFixture.SetupHelpers();

        given.WeHaveAWeatherReportRequest("bristol", DateTime.Now, out var apiRequest)
            .And.TheServersAreStarted();

        when.WeSendTheMessageToTheApi(apiRequest, out var response);

        then.TheResponseCodeShouldBe(response, HttpStatusCode.OK)
            .And.TheBodyShouldNotBeEmpty<WeatherReportResponse>(response, 
                x => Assert.That(x.Summary, Is.Not.Empty));
    }

    [Test]
    public void e2e_flow_notifications_sent_when_ModelingDataAccepted()
    {
        var (given, when, then) = testFixture.SetupHelpers();

        var testLocation = $"testLocation{Guid.NewGuid()}"[..20];
        var testReference = $"testRef{Guid.NewGuid()}"[..10];
        var requestId = Guid.NewGuid();

        given.WeHaveSomeCollectedWeatherData(out var weatherData)
            .And.TheContributorPaymentsServicePendingEndpointWillReturn(HttpStatusCode.Accepted)
            .And.TheModelingServiceSubmitEndpointWillReturn(HttpStatusCode.Accepted)
            .And.TheServersAreStarted();
        
        when.InPhase("1 (initial API request)") 
            .And.WeWrapTheCollectedWeatherDataInAnHttpRequestMessage(weatherData, testLocation, testReference, requestId, out var httpRequest)
            .And.WeSendTheMessageToTheApi(httpRequest, out var response);

        then.And.TheModelingServiceSubmitEndpointShouldHaveBeenCalled(times: 1)
            .And.TheEventShouldHaveBeenPersisted<SubmittedToModeling>()
            .And.TheResponseCodeShouldBe(response, HttpStatusCode.OK)
            .And.TheBodyShouldNotBeEmpty<WeatherDataCollectionResponse>(response)
            .And.WeGetTheStreamIdFromTheInitialDomainEvent(requestId, out var streamId);
        
        when.InPhase("2 (1st ASB message back from modeling service)")
            .AMessageAppears(message: new ModelingDataAcceptedIntegrationEvent(streamId));

        then.TheEventShouldHaveBeenPersisted<ModelingDataAccepted>();

        when.InPhase("3 (2nd ASB message back from modeling service)")
            .AMessageAppears(message: new ModelUpdatedIntegrationEvent(streamId));

        then.TheEventShouldHaveBeenPersisted<ModelUpdated>()
            .And.AnOutboxRecordWasInserted<UserNotificationEvent>();

        then.InPhase("4 (Notification Service handles event dispached by outbox)")
            .AfterSomeTimeHasPassed(2_000, 1_000)
            .And.TheMessageWasHandled<ModelUpdatedIntegrationEvent>()
            .And.TheNotificationServiceNotifiedTheUser(testLocation, testReference);
    }

    [Test]
    public void ApiPhase_EndsInConflict_IfExistingAggregateFound_WithSameRequestIdButDifferentReference()
    {
        var (given, when, then) = testFixture.SetupHelpers();

        var testLocation = $"testLocation{Guid.NewGuid()}"[..20];
        var testReference = $"testRef{Guid.NewGuid()}"[..10];
        var requestId = Guid.NewGuid();

        given.WeHaveSomeCollectedWeatherData(out var weatherData)
            .And.WeHaveResetEverything()
            .And.ThereIsExistingData(CannedData.UpTo_WeatherDataCollectionInitiated(testLocation, "differentReference", requestId.ToString()))
            .And.TheServersAreStarted();
        
        when.InPhase("1 (initial API request)") 
            .And.WeWrapTheCollectedWeatherDataInAnHttpRequestMessage(weatherData, testLocation, testReference, requestId, out var httpRequest)
            .And.WeSendTheMessageToTheApi(httpRequest, out var response);

        then.And.TheModelingServiceSubmitEndpointShouldNotHaveBeenCalled()
            .And.TheResponseCodeShouldBe(response, HttpStatusCode.Conflict);
    }

    [Test]
    public void ApiPhase_EndsInSameFailure_IfExistingAggregateFound_WhichEndedInAPermanentFailure()
    {
        var (given, when, then) = testFixture.SetupHelpers();

        var testLocation = $"testLocation{Guid.NewGuid()}"[..20];
        var testReference = $"testRef{Guid.NewGuid()}"[..10];
        var requestId = Guid.NewGuid();

        given.WeHaveSomeCollectedWeatherData(out var weatherData)
            .And.WeHaveResetEverything()
            .And.ThereIsExistingData(CannedData.UpTo_WeatherDataCollectionInitiated(testLocation, "differentReference", requestId.ToString()))
            .And.TheServersAreStarted();
        
        when.InPhase("1 (initial API request)") 
            .And.WeWrapTheCollectedWeatherDataInAnHttpRequestMessage(weatherData, testLocation, testReference, requestId, out var httpRequest)
            .And.WeSendTheMessageToTheApi(httpRequest, out var response);

        then.And.TheModelingServiceSubmitEndpointShouldNotHaveBeenCalled()
            .And.TheResponseCodeShouldBe(response, HttpStatusCode.Conflict);
    }

    [TearDown]
    public void TearDown()
    {
        testFixture.Dispose();
    }
}