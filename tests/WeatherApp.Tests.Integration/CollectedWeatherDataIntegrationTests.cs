﻿using System.Data;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;

namespace WeatherApp.Tests.Integration;

public class CollectedWeatherDataIntegrationTests
{
    private HttpClient client = null!;
    private IDbConnection connection = null!;

    [SetUp]
    public void Setup()
    {
        IConfigurationRoot config = new ConfigurationBuilder()
            .AddUserSecrets<CollectedWeatherDataIntegrationTests>()
            .Build();

        client = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7220")
        };

        var connectionString = config["WeatherAppDbConnectionString"];        
        connection = new SqlConnection(connectionString);
    }    

    [Test]
    public async Task PostCollectedWeatherData_ShouldSaveToDatabase()
    {
        // Arrange
        var location = "TestLocation";
        var reference = "TestReference";
        var collectedWeatherData = new CollectedWeatherDataModel(
            new List<CollectedWeatherDataPointModel>
                {
                    new CollectedWeatherDataPointModel(
                        DateTimeOffset.UtcNow,
                        10.5m,
                        "N",
                        25.3m,
                        60.2m)
                });

        // Act
        var response = await client.PostAsJsonAsync(
            $"/v1/collected-weather-data/{location}/{reference}", collectedWeatherData);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var weatherReportResponse = await response.Content.ReadFromJsonAsync<WeatherReportResponse>();
        var streamId = weatherReportResponse?.RequestId;

        var domainEvents = await connection.QueryAsync<dynamic>(
            "SELECT TOP 20 * FROM DomainEvents WHERE StreamId = @StreamId",
            new { StreamId = streamId });

        Assert.That(domainEvents, Is.Not.Null);
        Assert.That(domainEvents.Count(), Is.GreaterThan(0));

        IEnumerable<dynamic>? outboxItems = default;
        for(var attempts = 0; attempts < 20; attempts++)
        {
            await Task.Delay(1000); // Wait for the outbox to be processed

            outboxItems = await connection.QueryAsync<dynamic>(
                "SELECT TOP 20 * FROM OutboxItems WHERE AssociatedId = @StreamId",
                new { AssociatedId = streamId });

            if (outboxItems.Any())
                break;
        }

        Assert.That(outboxItems, Is.Not.Null);
        Assert.That(outboxItems.Count(), Is.EqualTo(1));
        Assert.That(outboxItems.First().Status, Is.EqualTo("Pending"));
    }

    [TearDown]
    public void TearDown()
    {
        client.Dispose();
        connection.Dispose();
    }
}

public record CollectedWeatherDataPointModel(
    DateTimeOffset time,
    decimal WindSpeedInMetersPerSecond,
    string WindDirection,
    decimal TemperatureReadingInDegreesCelcius,
    decimal HumidityReadingInPercent
    );

public record CollectedWeatherDataModel(List<CollectedWeatherDataPointModel> Points);

public record WeatherReportResponse(string RequestedRegion, DateTime RequestedDate, Guid RequestId, int Temperature, string Summary);

/*
In a terminal, cd into the AppHost directory and run `dotnet run`
In docker/rancher you will see the containers coming up.
In the logs you will find the url of the Dashboard, where you can find database connection strings and urls etc
*/