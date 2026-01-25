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

        given.WeHaveSomeCollectedWeatherData(cannedData, out var weatherData, out var reference)
            .And.TheContributorPaymentsServiceCreateEndpointWillReturn(HttpStatusCode.OK)
            .And.TheContributorPaymentsServiceCommitEndpointWillReturn(HttpStatusCode.OK)
            .And.TheModelingServiceSubmitEndpointWillReturn(HttpStatusCode.Accepted)
            .And.TheServersAreStarted();
        
        when.InPhase("1 (initial API request)") 
            .And.WeWrapTheCollectedWeatherDataInAnHttpRequestMessage(weatherData, cannedData, reference, out var httpRequest)
            .And.WeSendTheMessageToTheApi(httpRequest, out var response);

        then.TheContributorPaymentsServiceCreateEndpointShouldHaveBeenCalled(times: 1)
            .And.TheModelingServiceSubmitEndpointShouldHaveBeenCalled(times: 1)
            .And.TheDomainEventShouldHaveBeenPersisted<SubmittedToModeling>()
            .And.TheResponseCodeShouldBe(response, HttpStatusCode.OK)
            .And.TheBodyShouldNotBeEmpty(response)
            .And.WeGetTheStreamIdFromTheInitialDomainEvent(cannedData.RequestId, out var streamId);
        
        when.InPhase("2 (1st ASB message back from modeling service)")
            .AMessageAppears(new ModelingDataAcceptedIntegrationEvent(streamId)); // Mimic Modeling Service.

        then.TheMessageWasHandled<ModelingDataAcceptedIntegrationEvent>()
            .And.TheDomainEventShouldHaveBeenPersisted<ModelingDataAccepted>()
            .And.TheContributorPaymentsServiceCommitEndpointShouldHaveBeenCalled(times: 1);

        when.InPhase("3 (2nd ASB message back from modeling service)")
            .AMessageAppears(new ModelUpdatedIntegrationEvent(streamId)); // Mimic Modeling Service.

        then.TheMessageWasHandled<ModelUpdatedIntegrationEvent>()
            .And.TheDomainEventShouldHaveBeenPersisted<ModelUpdated>()
            .And.WeShouldHaveTheCorrectNumberOfDomainEventsPersisted(8)
            .And.AnOutboxRecordWasInserted<UserNotificationEvent>();

        then.InPhase("4 (Outbox dispaches the UserNotificationEvent)")
            .AfterSomeTimeHasPassed()
            .AMessageWasSent<UserNotificationEvent>(@event => 
                @event.Reference == reference 
                && @event.Body == "Dear user, your data has been submitted and included in our latest model");
    }

    [Test]
    public void ApiPhase_EndsInConflict_IfExistingAggregateFound_WithSameRequestIdButDifferentReference()
    {
        var (given, when, then, cannedData) = testFixture.SetupHelpers();
        given.WeHaveSomeCollectedWeatherData(cannedData, out var weatherData, out var reference)
            .And.WeHaveResetEverything()
            .And.ThereIsExistingData(cannedData.UpTo_WeatherDataCollectionInitiated(reference: "differentReference"))
            .And.TheServersAreStarted();
        
        when.InPhase("1 (initial API request)") 
            .And.WeWrapTheCollectedWeatherDataInAnHttpRequestMessage(weatherData, cannedData, reference, out var httpRequest)
            .And.WeSendTheMessageToTheApi(httpRequest, out var response);

        then.And.TheModelingServiceSubmitEndpointShouldNotHaveBeenCalled()
            .And.TheResponseCodeShouldBe(response, HttpStatusCode.Conflict);
    }

    [Test]
    public void ApiPhase_EndsInSameFailure_IfExistingAggregateFound_WhichEndedInAPermanentFailure()
    {
        var (given, when, then, cannedData) = testFixture.SetupHelpers();
        given.WeHaveSomeCollectedWeatherData(cannedData, out var weatherData, out var reference)
            .And.WeHaveResetEverything()
            .And.ThereIsExistingData(cannedData.UpTo_WeatherModelingServiceRejectionFailure())
            .And.TheServersAreStarted();
        
        when.InPhase("1 (initial API request)") 
            .And.WeWrapTheCollectedWeatherDataInAnHttpRequestMessage(weatherData, cannedData, reference, out var httpRequest)
            .And.WeSendTheMessageToTheApi(httpRequest, out var response);

        then.And.TheModelingServiceSubmitEndpointShouldNotHaveBeenCalled()
            .And.TheResponseCodeShouldBe(response, HttpStatusCode.UnprocessableEntity)
            .And.TheBodyShouldNotBeEmpty<string>(response, 
                x => x.ShouldBe(cannedData.modelingDataRejectedReason));
    }

    [Test]
    public void e2e_flow_notifications_sent_when_ModelingDataAccepted_with_mimicing_of_NotificationService()
    {
        var (given, when, then, cannedData) = testFixture.SetupHelpers();
        UserNotificationEvent? handledUserNotificationEvent = null;

        given.WeHaveSomeCollectedWeatherData(cannedData, out var weatherData, out var reference)
            .And.TheContributorPaymentsServiceCreateEndpointWillReturn(HttpStatusCode.OK)
            .And.TheContributorPaymentsServiceCommitEndpointWillReturn(HttpStatusCode.OK)
            .And.TheModelingServiceSubmitEndpointWillReturn(HttpStatusCode.Accepted)
            .And.WeWillHandleAMessageOfType<UserNotificationEvent>( // Mimic Notification Service.
                @event => handledUserNotificationEvent = @event)
            .And.TheServersAreStarted();
        
        when.InPhase("1 (initial API request)") 
            .And.WeWrapTheCollectedWeatherDataInAnHttpRequestMessage(weatherData, cannedData, reference, out var httpRequest)
            .And.WeSendTheMessageToTheApi(httpRequest, out var response);

        then.TheContributorPaymentsServiceCreateEndpointShouldHaveBeenCalled(times: 1)
            .And.TheModelingServiceSubmitEndpointShouldHaveBeenCalled(times: 1)
            .And.TheDomainEventShouldHaveBeenPersisted<SubmittedToModeling>()
            .And.TheResponseCodeShouldBe(response, HttpStatusCode.OK)
            .And.TheBodyShouldNotBeEmpty(response)
            .And.WeGetTheStreamIdFromTheInitialDomainEvent(cannedData.RequestId, out var streamId);
        
        when.InPhase("2 (1st ASB message back from modeling service)")
            .AMessageAppears(new ModelingDataAcceptedIntegrationEvent(streamId)); // Mimic Modeling Service.

        then.TheMessageWasHandled<ModelingDataAcceptedIntegrationEvent>()
            .And.TheDomainEventShouldHaveBeenPersisted<ModelingDataAccepted>()
            .And.TheContributorPaymentsServiceCommitEndpointShouldHaveBeenCalled(times: 1);

        when.InPhase("3 (2nd ASB message back from modeling service)")
            .AMessageAppears(new ModelUpdatedIntegrationEvent(streamId)); // Mimic Modeling Service.

        then.TheMessageWasHandled<ModelUpdatedIntegrationEvent>()
            .And.TheDomainEventShouldHaveBeenPersisted<ModelUpdated>()
            .And.WeShouldHaveTheCorrectNumberOfDomainEventsPersisted(8)
            .And.AnOutboxRecordWasInserted<UserNotificationEvent>();

        then.InPhase("4 (Notification Service handles event dispached by outbox)")
            .AfterSomeTimeHasPassed()
            .AMessageWasSent<UserNotificationEvent>()
            .TheMessageWasHandled<UserNotificationEvent>()
            .AndAlso("The handled UserNotificationEvent properties were correct", () =>
            {
                handledUserNotificationEvent.ShouldNotBeNull();
                handledUserNotificationEvent!.Reference.ShouldBe(reference);
                handledUserNotificationEvent.Body.ShouldBe(
                    "Dear user, your data has been submitted and included in our latest model");
            });
    }

    [After(Test)]
    public void TearDown()
    {
        testFixture.Dispose();
    }
}