using Moq;
using OlesLogger;
using OlesLogger.LogOutputs.FileLogOutput;

namespace Tests;

public class FileLogOutputTests
{
    private string _testDirectory = null!;
    private string _logFilePath = null!;
    private FileLogOutputConfiguration _config = null!;
    private FileLogOutput _fileOutput = null!;

    [SetUp]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "FileLogOutputTests");
        Directory.CreateDirectory(_testDirectory);
        _logFilePath = Path.Combine(_testDirectory, "test.log");
        _config = new FileLogOutputConfiguration(_logFilePath);
        _fileOutput = new FileLogOutput(_config);
    }

    [TearDown]
    public async Task Cleanup()
    {
        await _fileOutput.DisposeAsync();
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Test]
    public async Task WriteEntryAsync_WritesMessageToFile()
    {
        // Arrange
        var entry = CreateLogEntry("Test message");

        // Act
        await _fileOutput.WriteEntryAsync(entry);
        await _fileOutput.DisposeAsync();

        // Assert
        var fileContent = await File.ReadAllTextAsync(_logFilePath);
        Assert.That(fileContent.Trim(), Is.EqualTo("Test message"));
    }

    [Test]
    public async Task WriteEntryAsync_WithSizeRolling_CreatesNewFileWhenSizeLimitReached()
    {
        // Arrange
        _config.SetRollingSizeLimitInMib(1);
        var largeEntry = CreateLogEntry(new string('x', 1024 * 1024));
        var rolledLogFile = Path.Combine(_testDirectory, "test.0.log");

        // Act
        await _fileOutput.WriteEntryAsync(largeEntry);
        var secondEntry = CreateLogEntry("Second file entry");
        await _fileOutput.WriteEntryAsync(secondEntry);
        await _fileOutput.DisposeAsync();

        // Assert
        Assert.That(File.Exists(_logFilePath), Is.True);
        Assert.That(File.Exists(rolledLogFile), Is.True);
        var secondFileContent = await File.ReadAllTextAsync(rolledLogFile);
        Assert.That(secondFileContent.Trim(), Is.EqualTo("Second file entry"));
    }

    [Test]
    public async Task WriteEntryAsync_WithTimeRolling_CreatesNewFileWhenIntervalPassed()
    {
        // Arrange
        _config.SetRollingInterval(TimeSpan.FromMilliseconds(100));
        var rolledLogFile = Path.Combine(_testDirectory, "test.0.log");
        
        // Act
        await _fileOutput.WriteEntryAsync(CreateLogEntry("First entry"));
        await Task.Delay(150);
        await _fileOutput.WriteEntryAsync(CreateLogEntry("Second entry"));
        await _fileOutput.DisposeAsync();

        // Assert
        Assert.That(File.Exists(_logFilePath), Is.True);
        Assert.That(File.Exists(rolledLogFile), Is.True);
        var secondFileContent = await File.ReadAllTextAsync(rolledLogFile);
        Assert.That(secondFileContent.Trim(), Is.EqualTo("Second entry"));
    }

    [Test]
    public async Task DisposeAsync_ClosesFileHandle()
    {
        // Arrange
        await _fileOutput.WriteEntryAsync(CreateLogEntry("Test message"));

        // Act
        await _fileOutput.DisposeAsync();

        // Assert
        Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await _fileOutput.WriteEntryAsync(CreateLogEntry("Should fail")));
    }

    [Test]
    public async Task WriteEntryAsync_MultipleConcurrentWrites_HandlesCorrectly()
    {
        // Arrange
        const int numberOfWrites = 100;
        var tasks = new List<Task>();
        var messages = Enumerable.Range(1, numberOfWrites)
            .Select(i => $"Message {i}")
            .ToList();

        // Act
        foreach (var message in messages)
        {
            tasks.Add(_fileOutput.WriteEntryAsync(CreateLogEntry(message)));
        }
        await Task.WhenAll(tasks);
        await _fileOutput.DisposeAsync();

        // Assert
        var fileContent = await File.ReadAllLinesAsync(_logFilePath);
        Assert.That(fileContent.Length, Is.EqualTo(numberOfWrites));
        foreach (var message in messages)
        {
            Assert.That(fileContent.Contains(message), Is.True);
        }
    }

    private static ILogEntry CreateLogEntry(string message)
    {
        var mockEntry = new Mock<ILogEntry>();
        mockEntry.Setup(e => e.GeneralFormattedMessage).Returns(message);
        return mockEntry.Object;
    }
}