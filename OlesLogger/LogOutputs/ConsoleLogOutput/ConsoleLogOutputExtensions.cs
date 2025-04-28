using OlesLogger.Configuration;

namespace OlesLogger.LogOutputs.ConsoleLogOutput;

public static class ConsoleLogOutputExtensions
{
    public static OlesLoggerConfiguration AddConsoleOutput(this OlesLoggerConfiguration loggerConfiguration)
    {
        return loggerConfiguration.AddLogOutput(new ConsoleLogOutput());
    }
}