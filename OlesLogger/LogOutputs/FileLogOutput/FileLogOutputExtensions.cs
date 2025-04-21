using OlesLogger.Configuration;

namespace OlesLogger.LogOutputs.FileLogOutput;

public static class FileLogOutputExtensions
{
    public static OlesLoggerConfiguration AddFileOutput(this OlesLoggerConfiguration configuration, Action<FileLogOutputConfiguration> fileLogOutputConfig)
    {
        var outputConfig = new FileLogOutputConfiguration();
        fileLogOutputConfig(outputConfig);
        return configuration.AddLogOutput(new FileLogOutput(outputConfig));
    }
    
    public static FileLogOutputConfiguration SetFilePath(this FileLogOutputConfiguration configuration, string filePath)
    {
        configuration.FilePath = filePath;
        return configuration;
    }

    public static FileLogOutputConfiguration SetRollingInterval(this FileLogOutputConfiguration configuration, TimeSpan interval)
    {
        configuration.RollingInterval = interval;
        return configuration;
    }

    public static FileLogOutputConfiguration SetRollingSizeLimitInMib(this FileLogOutputConfiguration configuration, int fileSizeLimitInMib)
    {
        configuration.RollingSizeLimitMib = fileSizeLimitInMib;
        return configuration;
    }
}