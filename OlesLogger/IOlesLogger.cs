namespace OlesLogger;

public interface IOlesLogger : IAsyncDisposable
{
    void Write(LogLevels logLevel, string? messageTemplate, params ReadOnlySpan<object?> args);

    void Critical(string messageTemplate, params ReadOnlySpan<object?> args) => Write(LogLevels.Critical, messageTemplate, args);
    void Error(string messageTemplate, params ReadOnlySpan<object?> args) => Write(LogLevels.Error, messageTemplate, args);
    void Warning(string messageTemplate, params ReadOnlySpan<object?> args) => Write(LogLevels.Warning, messageTemplate, args);
    void Information(string messageTemplate, params ReadOnlySpan<object?> args) => Write(LogLevels.Information, messageTemplate, args);
    void Verbose(string messageTemplate, params ReadOnlySpan<object?> args) => Write(LogLevels.Verbose, messageTemplate, args);
}