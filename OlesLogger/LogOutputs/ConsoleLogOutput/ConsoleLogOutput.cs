using System.Text;

namespace OlesLogger.LogOutputs.ConsoleLogOutput;

public sealed class ConsoleLogOutput(bool showDetails = false) : ILogOutput
{
    public ValueTask WriteEntryAsync(ILogEntry logEntry)
    {
        Console.WriteLine(ShowDetails ? GetLogEntryDetails(logEntry) : logEntry.FinalFormattedMessage);
        return ValueTask.CompletedTask;
    }

    internal bool ShowDetails { get; set; } = showDetails;
    
    private string GetLogEntryDetails(ILogEntry logEntry)
    {
        var builder = new StringBuilder();
        builder.AppendLine(logEntry.FinalFormattedMessage)
            .Append('{')
            .Append('\t').AppendLine($"""
                                      "{nameof(logEntry.TimeStamp)}": "{logEntry.TimeStamp}"
                                      """)
            .Append('\t').AppendLine($"""
                                      "{nameof(logEntry.LogLevel)}": "{logEntry.LogLevel}"
                                      """)
            .Append('\t').AppendLine($"""
                                      "{nameof(logEntry.MessageTemplate)}": "{logEntry.MessageTemplate}"
                                      """)
            .Append('\t').AppendLine($"""
                                      "{nameof(logEntry.FormattedMessage)}": "{logEntry.FormattedMessage}"
                                      """)
            .Append('\t').AppendLine($"""
                                      "{nameof(logEntry.FinalFormattedMessage)}": "{logEntry.FinalFormattedMessage}"
                                      """)
            .Append('\t').Append($"""
                                  "{nameof(logEntry.Arguments)}": [
                                  """);
        foreach (var keyValue in logEntry.Arguments)
        {
            builder.AppendLine().Append('\t').Append('\t').AppendLine($"""
                                                                       "{keyValue.key}": {keyValue.value},
                                                                       """);
        }

        builder.Append('\t').AppendLine("]")
            .Append('}');
        
        return builder.ToString();
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}