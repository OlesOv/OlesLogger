namespace OlesLogger;

public class ConsoleLogOutput : ILogOutput
{
    public void WriteEntry(ILogEntry entry)
    {
        Console.WriteLine(entry.DisplayString);
    }
}