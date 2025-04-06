using System.Text;
using System.Text.Json;

namespace OlesLogger;

public interface ILogEntry
{
    DateTimeOffset TimeStamp { get; init; }
    LogLevel LogLevel { get; init; }
    string Template { get; set; }
    string FormattedMessage { get; set; }
    string GeneralFormattedMessage { get; set; }
    IDictionary<string, object?> Arguments { get; set; }

    string DisplayString
    {
        get
        {
            var builder = new StringBuilder();
            builder.AppendLine(GeneralFormattedMessage)
                .Append('{')
                .Append('\t').AppendLine($"""
                                          "{nameof(TimeStamp)}": "{TimeStamp}"
                                          """)
                .Append('\t').AppendLine($"""
                                          "{nameof(LogLevel)}": "{LogLevel}"
                                          """)
                .Append('\t').AppendLine($"""
                                          "{nameof(Template)}": "{Template}"
                                          """)
                .Append('\t').AppendLine($"""
                                          "{nameof(FormattedMessage)}": "{FormattedMessage}"
                                          """)
                .Append('\t').AppendLine($"""
                                          "{nameof(GeneralFormattedMessage)}": "{GeneralFormattedMessage}"
                                          """)
                .Append('\t').Append($"""
                                      "{nameof(Arguments)}": [
                                      """);
            foreach (var pair in Arguments)
            {
                builder.AppendLine().Append('\t').Append('\t').AppendLine($"""
                                                                           "{pair.Key}": {pair.Value},
                                                                           """);
            }

            builder.Append('\t').AppendLine("]")
                .Append('}');
            return builder.ToString();
        }
    }
}