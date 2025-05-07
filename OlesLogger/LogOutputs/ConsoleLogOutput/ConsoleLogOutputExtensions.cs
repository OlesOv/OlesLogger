using OlesLogger.Configuration;

namespace OlesLogger.LogOutputs.ConsoleLogOutput;

public static class ConsoleLogOutputExtensions
{
    public static OlesLoggerConfiguration AddConsoleOutput(this OlesLoggerConfiguration loggerConfiguration, bool showDetails = false)
    {
        return loggerConfiguration.AddLogOutput(new ConsoleLogOutput(showDetails));
    }
}