using System.Text;

namespace OlesLogger.LogOutputs.ConsoleLogOutput;

public sealed class ConsoleLogOutput : ILogOutput
{
    public Task WriteEntryAsync(ILogEntry entry)
    {
        var builder = new StringBuilder();
        builder.AppendLine(entry.GeneralFormattedMessage)
            .Append('{')
            .Append('\t').AppendLine($"""
                                      "{nameof(entry.TimeStamp)}": "{entry.TimeStamp}"
                                      """)
            .Append('\t').AppendLine($"""
                                      "{nameof(entry.LogLevel)}": "{entry.LogLevel}"
                                      """)
            .Append('\t').AppendLine($"""
                                      "{nameof(entry.Template)}": "{entry.Template}"
                                      """)
            .Append('\t').AppendLine($"""
                                      "{nameof(entry.FormattedMessage)}": "{entry.FormattedMessage}"
                                      """)
            .Append('\t').AppendLine($"""
                                      "{nameof(entry.GeneralFormattedMessage)}": "{entry.GeneralFormattedMessage}"
                                      """)
            .Append('\t').Append($"""
                                  "{nameof(entry.Arguments)}": [
                                  """);
            foreach (var pair in entry.Arguments)
            {
                builder.AppendLine().Append('\t').Append('\t').AppendLine($"""
                                                                           "{pair.key}": {pair.value},
                                                                           """);
            }

        builder.Append('\t').AppendLine("]")
            .Append('}');
        
        Console.WriteLine(builder);
        return Task.CompletedTask;
    }
}