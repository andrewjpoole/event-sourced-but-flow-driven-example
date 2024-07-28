namespace WeatherApp.Domain.Outcomes;

public class UnsupportedRegionFailure(string region)
{
    public string Title => "Unsupported Region";

    public string Detail { get; } = $"{region} is not a supported region";
}