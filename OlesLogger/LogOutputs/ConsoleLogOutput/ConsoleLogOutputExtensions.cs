using OlesLogger.Configuration;

namespace OlesLogger.LogOutputs.ConsoleLogOutput;

public static class ConsoleLogOutputExtensions
{
    public static OlesLoggerConfiguration AddConsoleOutput(this OlesLoggerConfiguration configuration)
    {
        return configuration.AddLogOutput(new ConsoleLogOutput());
    }
}