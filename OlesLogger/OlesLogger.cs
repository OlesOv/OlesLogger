using System.Text;
using System.Text.RegularExpressions;
using OlesLogger.Configuration;

namespace OlesLogger;

public record struct GeneralTemplateFormatParameters(
    string GeneralTemplate,
    DateTimeOffset TimeStamp,
    LogLevel Level,
    string MessageTemplate,
    string FormattedMessage,
    string Arguments);

public partial class OlesLogger(OlesLoggerConfiguration configuration) : IOlesLogger
{
    private const string NullValue = "";

    [GeneratedRegex(@"\{(.*?)\}")]
    private static partial Regex GetParametersRegex();

    public void Write(LogLevel level, string? messageTemplate, params ReadOnlySpan<object?> args)
    {
        var logTimeStamp = DateTimeOffset.UtcNow;
        messageTemplate ??= "";
        var appliedTemplate = ParseAndApplyMessageTemplate(messageTemplate, args);
        string generalFormattedMessage = GetMessageWithGeneralTemplate(new GeneralTemplateFormatParameters()
        {
            GeneralTemplate = configuration.GeneralFormat,
            TimeStamp = logTimeStamp,
            Level = level,
            MessageTemplate = messageTemplate,
            FormattedMessage = appliedTemplate.formattedMessage,
            Arguments = appliedTemplate.arguments.ToString() ?? "[]"
        });
        var entry = new LogEntry
        {
            TimeStamp = logTimeStamp,
            LogLevel = level,
            Template = messageTemplate,
            FormattedMessage = appliedTemplate.formattedMessage,
            Arguments = appliedTemplate.arguments,
            GeneralFormattedMessage = generalFormattedMessage
        };

        Task.WhenAll(
            configuration.Outputs.Select(logOutput => logOutput.WriteEntryAsync(entry))).GetAwaiter().GetResult();
    }


    private (string formattedMessage, IList<(string key, object? value)> arguments) ParseAndApplyMessageTemplate(
        string messageTemplate,
        ReadOnlySpan<object?> arguments)
    {
        var argumentsList = new List<(string key, object? value)>();
        var formattedMessage = new StringBuilder(messageTemplate);
        var matches = GetParametersRegex().Matches(messageTemplate);
        for (int i = 0; i < matches.Count; i++)
        {
            Match match = matches[i];
            var keyName = match.Groups[1].Value;
            if (i >= arguments.Length)
            {
                argumentsList.Add((keyName, null));
                continue;
            }

            argumentsList.Add((keyName, arguments[i]));
            var replacementValue = arguments[i]?.ToString() ?? NullValue;
            int index = formattedMessage.ToString().IndexOf(match.Groups[0].Value, StringComparison.Ordinal);

            formattedMessage.Remove(index, match.Groups[0].Value.Length).Insert(index, replacementValue);
        }

        return (formattedMessage.ToString(), argumentsList);
    }

    private static string GetMessageWithGeneralTemplate(GeneralTemplateFormatParameters generalTemplateFormatParameters)
    {
        return generalTemplateFormatParameters.GeneralTemplate.Replace("{TimeStamp}",
                generalTemplateFormatParameters.TimeStamp.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                StringComparison.InvariantCultureIgnoreCase)
            .Replace("{LogLevel}", generalTemplateFormatParameters.Level.ToString(),
                StringComparison.InvariantCultureIgnoreCase)
            .Replace("{Template}", generalTemplateFormatParameters.MessageTemplate,
                StringComparison.InvariantCultureIgnoreCase)
            .Replace("{FormattedMessage}", generalTemplateFormatParameters.FormattedMessage,
                StringComparison.InvariantCultureIgnoreCase)
            .Replace("{Arguments}", generalTemplateFormatParameters.Arguments,
                StringComparison.InvariantCultureIgnoreCase);
    }
}