using System.Text;
using System.Text.RegularExpressions;
using OlesLogger.Configuration;

namespace OlesLogger;

public record struct FinalFormatParameters(
    string FinalFormatTemplate,
    DateTimeOffset TimeStamp,
    LogLevels LogLevel,
    string MessageTemplate,
    string FormattedMessage,
    string Arguments);

public partial class OlesLogger(OlesLoggerConfiguration configuration) : IOlesLogger
{
    private const string NullValue = "";

    [GeneratedRegex(@"\{(.*?)\}")]
    private static partial Regex GetParametersRegex();

    public void Write(LogLevels logLevel, string? messageTemplate, params ReadOnlySpan<object?> args)
    {
        var logTimeStamp = DateTimeOffset.UtcNow;
        messageTemplate ??= "";
        var appliedTemplate = ParseAndApplyMessageTemplate(messageTemplate, args);
        string finalFormattedMessage = GetFinalFormattedString(new FinalFormatParameters()
        {
            FinalFormatTemplate = configuration.FinalFormatTemplate,
            TimeStamp = logTimeStamp,
            LogLevel = logLevel,
            MessageTemplate = messageTemplate,
            FormattedMessage = appliedTemplate.formattedMessage,
            Arguments = appliedTemplate.arguments.ToString() ?? "[]"
        });
        var entry = new LogEntry
        {
            TimeStamp = logTimeStamp,
            LogLevel = logLevel,
            MessageTemplate = messageTemplate,
            FormattedMessage = appliedTemplate.formattedMessage,
            Arguments = appliedTemplate.arguments,
            FinalFormattedMessage = finalFormattedMessage
        };

        Task.WhenAll(
            configuration.Outputs.Select(logOutput => logOutput.WriteEntryAsync(entry).AsTask())).GetAwaiter().GetResult();
    }


    private (string formattedMessage, IList<(string key, object? value)> arguments) ParseAndApplyMessageTemplate(
        string messageTemplate,
        ReadOnlySpan<object?> arguments)
    {
        var argumentsList = new List<(string key, object? value)>();
        var formattedMessage = new StringBuilder(messageTemplate);
        var matches = GetParametersRegex().Matches(messageTemplate);
        for (int matchImdex = 0; matchImdex < matches.Count; matchImdex++)
        {
            Match match = matches[matchImdex];
            var keyName = match.Groups[1].Value;
            if (matchImdex >= arguments.Length)
            {
                argumentsList.Add((keyName, null));
                continue;
            }

            argumentsList.Add((keyName, arguments[matchImdex]));
            var replacementValue = arguments[matchImdex]?.ToString() ?? NullValue;
            int index = formattedMessage.ToString().IndexOf(match.Groups[0].Value, StringComparison.Ordinal);

            formattedMessage.Remove(index, match.Groups[0].Value.Length).Insert(index, replacementValue);
        }

        return (formattedMessage.ToString(), argumentsList);
    }

    private static string GetFinalFormattedString(FinalFormatParameters finalFormatParameters)
    {
        return finalFormatParameters.FinalFormatTemplate.Replace("{TimeStamp}",
                finalFormatParameters.TimeStamp.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                StringComparison.InvariantCultureIgnoreCase)
            .Replace("{LogLevel}", finalFormatParameters.LogLevel.ToString(),
                StringComparison.InvariantCultureIgnoreCase)
            .Replace("{Template}", finalFormatParameters.MessageTemplate,
                StringComparison.InvariantCultureIgnoreCase)
            .Replace("{FormattedMessage}", finalFormatParameters.FormattedMessage,
                StringComparison.InvariantCultureIgnoreCase)
            .Replace("{Arguments}", finalFormatParameters.Arguments,
                StringComparison.InvariantCultureIgnoreCase);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var logOutput in configuration.Outputs)
        {
            await logOutput.DisposeAsync();
        }
        
    }
}