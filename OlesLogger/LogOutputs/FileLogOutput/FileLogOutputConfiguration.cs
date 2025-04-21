namespace OlesLogger.LogOutputs.FileLogOutput;

public class FileLogOutputConfiguration
{
    private string _filePath = null!;

    public FileLogOutputConfiguration()
    {
    }

    public FileLogOutputConfiguration(string filePath)
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
        get => _filePath;
        set
        {
            _filePath = value;
            ReinitConfig(_filePath);
        }
    }

    /// <summary>
    /// Specifies the timespan for rolling the log file. TimeSpan.Zero means the file never rolls based on time.
    /// </summary>
    internal TimeSpan RollingInterval { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Specifies the Size Limit in MiB for rolling the log file. 0 means the file never rolls based on size.
    /// </summary>
    internal int RollingSizeLimitMib { get; set; } = 0;

    internal bool Disposed { get; set; } = false;
    internal StreamWriter? Writer { get; set; }
    internal string CurrentFilePath { get; set; } = null!;
    internal string FilePathTemplate { get; private set; } = null!;
    internal string FileNameBase { get; private set; } = null!;
    internal int CurrentFileNumber { get; set; }
    internal DateTimeOffset LastRollDateTimeOffset { get; set; }
}