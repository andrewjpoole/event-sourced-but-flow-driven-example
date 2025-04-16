public class TraceData
{
    public string Resource { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public string SpanId { get; set; } = string.Empty;    
    public Dictionary<string, object?> Tags { get; set; } = new();
}
