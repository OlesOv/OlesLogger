using System.Collections.Concurrent;

namespace OlesLogger.LogOutputs.FileLogOutput;

public class FileLogOutputConfigurationFactory
{
    private readonly ConcurrentDictionary<string, FileLogOutputConfiguration> _fileLogOutputConfigurations = new();
    public FileLogOutputConfiguration GetConfiguration(string logFilePath)
    {
        if (_fileLogOutputConfigurations.TryGetValue(logFilePath, out var fileLogOutputConfiguration))
        {
            return fileLogOutputConfiguration;
        }

        var newFileOutputConfig =  new FileLogOutputConfiguration(logFilePath);
        _fileLogOutputConfigurations.AddOrUpdate(logFilePath, newFileOutputConfig, (k, v) => newFileOutputConfig);
        return newFileOutputConfig;
    }
}