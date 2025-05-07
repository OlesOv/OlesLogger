using System.Collections.Concurrent;

namespace OlesLogger.LogOutputs.FileLogOutput;

public class FileLogOutputConfiguration()
{
    private string? _logFilePath;

    public FileLogOutputConfiguration(string filePath) : this()
    {
        ReinitConfig(filePath);
    }

    private void ReinitConfig(string logFilePath)
    {
        if (string.IsNullOrWhiteSpace(logFilePath))
        {
            throw new ArgumentException("File path cannot be null or whitespace.", nameof(logFilePath));
        }

        var logFileDirectory = Path.GetDirectoryName(logFilePath);
        if (logFileDirectory == null) throw new ArgumentException("Incorrect log file path.", nameof(logFilePath));
        Directory.CreateDirectory(logFileDirectory);

        LogFilePath = logFilePath;

        CurrentLogFileNumber = 0;

        LogFileNameBase = Path.GetFileNameWithoutExtension(LogFilePath);
        LogFilePathTemplate = LogFilePath.Replace(LogFileNameBase, "{0}.{1}");
        CurrentLogFilePath = LogFilePath;
    }

    internal string LogFilePath
    {
        get => _logFilePath ?? "";
        set
        {
            if (_logFilePath != null) return;
            _logFilePath = value;
            ReinitConfig(_logFilePath);
        }
    }

    /// <summary>
    /// Specifies the timespan for rolling the log file.
    /// Set to TimeSpan.Zero to never roll based on time.
    /// </summary>
    internal TimeSpan RollingInterval { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Specifies the Size Limit in MiB for rolling the log file.
    /// Set to 0 to never roll based on size.
    /// </summary>
    internal int RollingSizeLimitMib { get; set; } = 0;

    /// <summary>
    /// Maximum number of log entries to buffer before writing to file. Default is 10.
    /// </summary>
    internal int BufferCountLimit { get; set; } = 10;

    /// <summary>
    /// Maximum time in milliseconds to hold entries in buffer before writing to file.
    /// Default is 1000ms (1 second). Set to 0 to disable time-based flushing.
    /// </summary>
    internal int FlushFrequencyMs { get; set; } = 1000;

    /// <summary>
    /// Maximum total size of buffered entries in bytes before writing to file.
    /// Default is 16KB. Set to 0 to disable size-based flushing.
    /// </summary>
    internal int BufferSizeLimitInBytes { get; set; } = 16 * 1024;

    internal bool Disposed { get; set; } = false;

    internal StreamWriter? Writer { get; set; }

    internal readonly BlockingCollection<ILogEntry> LogEntryQueue = new();
    internal string CurrentLogFilePath { get; set; } = null!;
    internal string LogFilePathTemplate { get; private set; } = null!;
    internal string LogFileNameBase { get; private set; } = null!;
    internal int CurrentLogFileNumber { get; set; }
    internal DateTimeOffset LogFileLastRolled { get; set; }
}