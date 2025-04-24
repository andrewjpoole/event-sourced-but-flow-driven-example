using System.Text;
using System.Text.Json;
using WeatherApp.Application.Models.Requests;
using WeatherApp.Domain.EventSourcing;

namespace WeatherApp.Tests.Framework;

public class When(ComponentTestFixture fixture)
{
    public When And => this;

    public When InPhase(string newPhase)
    {
        fixture.SetPhase(newPhase);
        return this;
    }

    public When WeSendTheMessageToTheApi(HttpRequestMessage httpRequest, out HttpResponseMessage response)
    {
        if (fixture.ApiFactory.HttpClient is null)
            throw new Exception("The Http client has not been initialised, please ensure Given.TheServerHasStarted() has been called");

        response = fixture.ApiFactory.HttpClient.SendAsync(httpRequest).GetAwaiter().GetResult();

        return this;
    }

    public When WeWrapTheCollectedWeatherDataInAnHttpRequestMessage(CollectedWeatherDataModel collectedWeatherDataModel, CannedData cannedData, out HttpRequestMessage httpRequest)
    {
        httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{Constants.WeatherModelingServiceSubmissionUri}{cannedData.Location}/{cannedData.Reference}");
        httpRequest.Headers.Add("x-request-id", cannedData.RequestId.ToString());
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(collectedWeatherDataModel, GlobalJsonSerialiserSettings.Default), Encoding.UTF8, "application/json");

        return this;
    }

    public When AMessageAppears<T>(T message) where T : class
    {
        var processor = fixture.FakeServiceBus.GetProcessorFor<T>();
        processor.PresentMessage(message).GetAwaiter().GetResult();

        return this;
    }
}