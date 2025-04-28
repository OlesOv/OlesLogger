namespace OlesLogger;

public interface ILogEntry
{
    DateTimeOffset TimeStamp { get; init; }
    LogLevels LogLevel { get; init; }
    string MessageTemplate { get; set; }
    string FormattedMessage { get; set; }
    string FinalFormattedMessage { get; set; }
    IList<(string key, object? value)> Arguments { get; set; }
}