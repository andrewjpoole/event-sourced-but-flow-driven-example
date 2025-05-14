namespace Microsoft.Extensions.Hosting;

public class ActivityLite
{
    public DateTimeOffset StartTimeUtc { get; set; } = TimeProvider.System.GetUtcNow();
    public string Resource { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public string SpanId { get; set; } = string.Empty;    
    public Dictionary<string, object?> Tags { get; set; } = new();
}