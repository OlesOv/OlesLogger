namespace OlesLogger;

public interface ILogger
{
    void Write(LogLevel level, string? format, params ReadOnlySpan<object?> args);
}