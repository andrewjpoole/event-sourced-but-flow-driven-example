using System.Collections.Concurrent;

namespace Aspire.QueryableTraceCollector.Integration;

public class CollectedData(ILogger<CollectedData> logger)
{
    private readonly ConcurrentBag<ActivityLite> activities = new();

    public void Import(in List<ActivityLite> batch)
    {
        foreach (var activity in batch)
        {
            logger.LogInformation(activity.ToString());
            activities.Add(activity);
        }
    }

    public IReadOnlyCollection<ActivityLite>? GetTraces() => activities.ToArray();
    public IReadOnlyCollection<ActivityLite>? GetTraces(Func<ActivityLite, bool> match) => activities.Where(x => match(x) == true).ToArray();
    
    public ActivityLite? GetTrace(string displayName) => activities.FirstOrDefault(x => x.DisplayName == displayName);

    public int Count() => activities.Count;

    public void Clear() => activities.Clear();    
}
