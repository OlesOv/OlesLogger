namespace OlesLogger.LogOutputs.FileLogOutput;

public class FileLogOutputConfiguration()
{
    private string? _filePath;

    public FileLogOutputConfiguration(string filePath) : this()
    {
        ReinitConfig(filePath);
    }

    private void ReinitConfig(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));
        }

        var directory = Path.GetDirectoryName(filePath);
        if (directory == null) throw new ArgumentException("Incorrect log file path.", nameof(filePath));
        Directory.CreateDirectory(directory);

        FilePath = filePath;

        CurrentFileNumber = 0;

        FileNameBase = Path.GetFileNameWithoutExtension(FilePath);
        FilePathTemplate = FilePath.Replace(FileNameBase, "{0}.{1}");
        CurrentFilePath = FilePath;
    }

    internal string FilePath
    {
        get => _filePath ?? "";
        set
        {
            if (_filePath != null) return;
            _filePath = value;
            ReinitConfig(_filePath);
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
    internal int BufferTimeoutMs { get; set; } = 1000;

    /// <summary>
    /// Maximum total size of buffered entries in bytes before writing to file.
    /// Default is 16KB. Set to 0 to disable size-based flushing.
    /// </summary>
    internal int BufferSizeLimit { get; set; } = 16 * 1024;

    internal bool Disposed { get; set; } = false;
    internal StreamWriter? Writer { get; set; }
    internal string CurrentFilePath { get; set; } = null!;
    internal string FilePathTemplate { get; private set; } = null!;
    internal string FileNameBase { get; private set; } = null!;
    internal int CurrentFileNumber { get; set; }
    internal DateTimeOffset LastRollDateTimeOffset { get; set; }
}