using System.Text;
using System.Text.RegularExpressions;

namespace OlesLogger;

public partial class Logger : ILogger
{
    private const string NullValue = "";
    public void Write(LogLevel level, string? format, params ReadOnlySpan<object?> args)
    {
        var logTimeStamp = DateTimeOffset.UtcNow; 
        format ??= "";
        foreach (var logOutput in Outputs)
        {
            var appliedTemplate = ParseTemplate(format, args);
            var entry = new LogEntry
            {
                TimeStamp = logTimeStamp,
                Template = format,
                FormattedMessage = appliedTemplate.formattedMessage,
                Arguments = appliedTemplate.arguments,
            };
            logOutput.WriteEntry(entry);
        }
    }

    private (string formattedMessage, IDictionary<string, object?> arguments) ParseTemplate(string messageTemplate, ReadOnlySpan<object?> arguments)
    {
        var argumentsDict = new Dictionary<string, object?>();
        var formattedMessage = new StringBuilder(messageTemplate);
        var matches = MyRegex().Matches(messageTemplate);
        for (int i = 0; i< matches.Count; i++)
        {
            Match match = matches[i];
            if(i > arguments.Length) argumentsDict.Add(match.Groups[0].Value, NullValue);
            
            argumentsDict.Add(match.Groups[0].Value, arguments[i]);
            formattedMessage.Replace(match.Groups[0].Value, argumentsDict[match.Groups[0].Value]?.ToString() ?? NullValue);
        }
        return (formattedMessage.ToString(), argumentsDict);
    }
    private IEnumerable<ILogOutput> Outputs { get; } = new List<ILogOutput>();

    [GeneratedRegex(@"\{(.*?)\}")]
    private static partial Regex MyRegex();
}