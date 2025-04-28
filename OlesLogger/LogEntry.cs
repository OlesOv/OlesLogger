namespace OlesLogger;

public class LogEntry : ILogEntry
{
    public DateTimeOffset TimeStamp { get; init; }
    public LogLevels LogLevel { get; init; }
    public string MessageTemplate { get; set; } = "";
    public string FormattedMessage { get; set; } = "";
    public string FinalFormattedMessage { get; set; } = "";
    public IList<(string key, object? value)> Arguments { get; set; } = new List<(string key, object? value)>();
}