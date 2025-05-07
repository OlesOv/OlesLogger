namespace OlesLogger.LogOutputs;

public interface ILogOutput : IAsyncDisposable
{
    ValueTask WriteEntryAsync(ILogEntry logEntry);
}