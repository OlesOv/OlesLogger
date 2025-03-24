namespace OlesLogger;

public class LogEntry : ILogEntry
{
    public DateTimeOffset TimeStamp { get; init; }
    public string Template { get; set; } = "";
    public string FormattedMessage { get; set; } = "";
    public IDictionary<string, object?> Arguments { get; set; } = new Dictionary<string, object?>();
}