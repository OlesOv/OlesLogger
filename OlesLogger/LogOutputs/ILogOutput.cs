namespace OlesLogger.LogOutputs;

public interface ILogOutput
{
    Task WriteEntryAsync(ILogEntry entry);
}