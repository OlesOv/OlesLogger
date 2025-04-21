namespace OlesLogger;

public class LogEntry : ILogEntry
{
    public DateTimeOffset TimeStamp { get; init; }
    public LogLevel LogLevel { get; init; }
    public string Template { get; set; } = "";
    public string FormattedMessage { get; set; } = "";
    public string GeneralFormattedMessage { get; set; } = "";
    public IList<(string key, object? value)> Arguments { get; set; } = new List<(string key, object? value)>();
}