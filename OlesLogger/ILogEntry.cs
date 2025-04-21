namespace OlesLogger;

public interface ILogEntry
{
    DateTimeOffset TimeStamp { get; init; }
    LogLevel LogLevel { get; init; }
    string Template { get; set; }
    string FormattedMessage { get; set; }
    string GeneralFormattedMessage { get; set; }
    IList<(string key, object? value)> Arguments { get; set; }
}