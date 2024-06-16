using System.Net;
using FluentAssertions;
using WeatherApp.Application.Models;
using WeatherApp.Application.Models.IntegrationEvents.WeatherModelingEvents;
using WeatherApp.Application.Models.Requests;
using WeatherApp.Domain.DomainEvents;
using WeatherApp.Tests.Framework;

namespace WeatherApp.Tests;

[Collection(nameof(NonParallelCollectionDefinition))]
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
    public void Return_accepted_for_valid_request()
    {
        var (given, when, then) = testFixture.SetupHelpers();

        given.WeHaveSomeCollectedWeatherData(out var weatherData)
            .And.TheModelingServiceSubmitEndpointWillReturn(HttpStatusCode.Accepted)
            .And.TheServersAreStarted();
        
        when.InPhase("1 (initial API request)") 
            .And.WeWrapTheCollectedWeatherDataInAnHttpRequestMessage(weatherData, "testLocation", out var httpRequest)
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

        then.TheEventShouldHaveBeenPersisted<ModelUpdated>();

    }
}