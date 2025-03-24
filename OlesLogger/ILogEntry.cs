namespace OlesLogger;

public interface ILogEntry
{
    DateTimeOffset TimeStamp { get; init; }
    string Template{get;set;}
    string FormattedMessage{get;set;}
    IDictionary<string, object?> Arguments{get;set;}
}