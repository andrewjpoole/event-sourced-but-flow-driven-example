namespace WeatherApp.Tests.Aspire.Integration;

public static class TracesAssertionsExtensions
{
    public static void AssertContains(this List<TraceData> traces, 
        Predicate<TraceData> predicate, string? message = null) => 
        Assert.That(traces, Has.Exactly(1).Matches(predicate), message);

    public static void AssertContainsDisplayName(this List<TraceData> traces, string displayName) => 
        traces.AssertContains(x => x.DisplayName == displayName, $"None of the {traces.Count} traces examined had a DisplayName of '{displayName}'.");
   
    public static void AssertContainsDomainEventInsertionTag(this List<TraceData> traces, string domainEventName) => 
        traces.AssertContains(x => 
        {
            if(x.DisplayName != "Domain Event Insertion")
                return false;
            
                if(x.Tags.ContainsKey("domain-event.eventclassName") == false) 
                    return false;

            var domainEventTagValue = x.Tags["domain-event.eventclassName"]?.ToString();

            if (string.IsNullOrEmpty(domainEventTagValue))
                return false;

            if(domainEventTagValue.EndsWith(domainEventName) == false)
                return false;

            return true;
        }, $"None of the {traces.Count} traces examined had a DisplayName of 'Domain Event Insertion' and a tag of 'domain-event.eventclassName' with a value of '{domainEventName}'.");    

    public static bool ContainsTag(this TraceData traceData, string tagKey, Func<string, bool> matchTagValue)
    {
        Assert.That(traceData.Tags.ContainsKey(tagKey), Is.True, $"TraceData does not contain a tag with the key '{tagKey}'.");
        Assert.That(string.IsNullOrEmpty(traceData.Tags[tagKey]?.ToString()), Is.False, $"TraceData tag '{tagKey}' is null or empty.");

        var valueOfTag = traceData.Tags[tagKey]?.ToString() ?? string.Empty;
        var result = matchTagValue(valueOfTag);

        Assert.That(result, Is.True, $"TraceData tag '{tagKey}' does not match the expected value.");

        return true;
    }
}