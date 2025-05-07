using OlesLogger.Configuration;

namespace OlesLogger.LogOutputs.FileLogOutput;

public static class FileLogOutputExtensions
{
    private static readonly FileLogOutputConfigurationFactory FileLogOutputConfigurationFactory = new();
    public static OlesLoggerConfiguration AddFileOutput(this OlesLoggerConfiguration loggerConfig, string logFilePath,
        Action<FileLogOutputConfiguration> fileLogOutputConfig)
    {
        var logOutputConfig = FileLogOutputConfigurationFactory.GetConfiguration(logFilePath);
        fileLogOutputConfig(logOutputConfig);
        return loggerConfig.AddLogOutput(new FileLogOutput(logOutputConfig));
    }

    public static FileLogOutputConfiguration SetBufferMaxCapacity(this FileLogOutputConfiguration fileLogOutputConfig,
        int bufferSize)
    {
        fileLogOutputConfig.BufferCountLimit = bufferSize;
        return fileLogOutputConfig;
    }

    public static FileLogOutputConfiguration SetFlushFrequency(this FileLogOutputConfiguration fileLogOutputConfig,
        int timeoutMs)
    {
        fileLogOutputConfig.FlushFrequencyMs = timeoutMs;
        return fileLogOutputConfig;
    }

    public static FileLogOutputConfiguration SetRollingInterval(this FileLogOutputConfiguration fileLogOutputConfig,
        TimeSpan interval)
    {
        fileLogOutputConfig.RollingInterval = interval;
        return fileLogOutputConfig;
    }

    public static FileLogOutputConfiguration SetRollingSizeLimitInMib(this FileLogOutputConfiguration fileLogOutputConfig,
        int fileSizeLimitInMib)
    {
        fileLogOutputConfig.RollingSizeLimitMib = fileSizeLimitInMib;
        return fileLogOutputConfig;
    }
}