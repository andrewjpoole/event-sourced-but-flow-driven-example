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

    [After(Test)]
    public void TearDown()
    {
        testFixture.Dispose();
    }
}