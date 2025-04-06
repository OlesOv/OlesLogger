using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace OlesLogger;

public partial class OlesLogger : IOlesLogger
{
    private const string NullValue = "";
    private readonly string _generalFormat;

    public OlesLogger(IConfiguration configuration)
    {
        _generalFormat = (!string.IsNullOrWhiteSpace(configuration.GetSection("OlesLogger").GetSection("DefaultFormat").Value)
            ? configuration.GetSection("OlesLogger").GetSection("DefaultFormat").Value
            : "{FormattedMessage}")!;
    }

    [GeneratedRegex(@"\{(.*?)\}")]
    private static partial Regex MyRegex();

    public void Write(LogLevel level, string? messageTemplate, params ReadOnlySpan<object?> args)
    {
        var logTimeStamp = DateTimeOffset.UtcNow;
        messageTemplate ??= "";
        var appliedTemplate = ParseMessageTemplate(messageTemplate, args);
        var entry = new LogEntry
        {
            TimeStamp = logTimeStamp,
            LogLevel = level,
            Template = messageTemplate,
            FormattedMessage = appliedTemplate.formattedMessage,
            Arguments = appliedTemplate.arguments,
            GeneralFormattedMessage = GetMessageWithGeneralTemplate(_generalFormat, logTimeStamp, level, messageTemplate,
                appliedTemplate.formattedMessage, appliedTemplate.arguments.ToString() ?? "[]")
        };

        IOlesLogger.Outputs.ForEach(logOutput => logOutput.WriteEntry(entry));
    }


    private (string formattedMessage, IDictionary<string, object?> arguments) ParseMessageTemplate(
        string messageTemplate,
        ReadOnlySpan<object?> arguments)
    {
        var argumentsDict = new Dictionary<string, object?>();
        var formattedMessage = new StringBuilder(messageTemplate);
        var matches = MyRegex().Matches(messageTemplate);
        for (int i = 0; i < matches.Count; i++)
        {
            Match match = matches[i];
            var keyName = match.Groups[1].Value;
            if (i >= arguments.Length)
            {
                argumentsDict.Add(keyName, null);
                continue;
            }

            argumentsDict.Add(keyName, arguments[i]);
            formattedMessage.Replace(match.Groups[0].Value, argumentsDict[keyName]?.ToString() ?? NullValue);
        }

        return (formattedMessage.ToString(), argumentsDict);
    }

    private string GetMessageWithGeneralTemplate(string generalTemplate, DateTimeOffset timeStamp, LogLevel level,
        string messageTemplate, string formattedMessage, string arguments)
    {
        return generalTemplate.Replace("{TimeStamp}", timeStamp.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                StringComparison.InvariantCultureIgnoreCase)
            .Replace("{LogLevel}", level.ToString(), StringComparison.InvariantCultureIgnoreCase)
            .Replace("{Template}", messageTemplate, StringComparison.InvariantCultureIgnoreCase)
            .Replace("{FormattedMessage}", formattedMessage, StringComparison.InvariantCultureIgnoreCase)
            .Replace("{Arguments}", arguments, StringComparison.InvariantCultureIgnoreCase);
    }
}