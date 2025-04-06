namespace OlesLogger;

public interface IOlesLogger
{
    void Write(LogLevel level, string? messageTemplate, params ReadOnlySpan<object?> args);

    void Critical(string format, params ReadOnlySpan<object?> args) => Write(LogLevel.Critical, format, args);
    void Error(string format, params ReadOnlySpan<object?> args) => Write(LogLevel.Error, format, args);
    void Warning(string format, params ReadOnlySpan<object?> args) => Write(LogLevel.Warning, format, args);
    void Information(string format, params ReadOnlySpan<object?> args) => Write(LogLevel.Information, format, args);
    void Verbose(string format, params ReadOnlySpan<object?> args) => Write(LogLevel.Verbose, format, args);
    // TODO: fix this static nightmare
    protected static List<ILogOutput> Outputs { get; } = [];
    static void AddLogOutput(ILogOutput logOutput) => Outputs.Add(logOutput);
}