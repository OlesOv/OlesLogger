namespace OlesLogger;

public interface ILogOutput
{
    void WriteEntry(ILogEntry entry);
}