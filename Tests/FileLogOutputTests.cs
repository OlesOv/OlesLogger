using Moq;
using OlesLogger;
using OlesLogger.LogOutputs.FileLogOutput;
using System.Collections.Concurrent;

namespace Tests;

public class FileLogOutputTests
{
    private string _testDirectory = null!;
    private string _logFilePath = null!;
    private FileLogOutputConfigurationFactory  _fileLogOutputConfigurationFactory = null!;
    private FileLogOutputConfiguration _config = null!;
    private FileLogOutput _fileOutput = null!;

    [SetUp]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "FileLogOutputTests");
        Directory.CreateDirectory(_testDirectory);
        _logFilePath = Path.Combine(_testDirectory, "test.log");
        _fileLogOutputConfigurationFactory  = new FileLogOutputConfigurationFactory();
        _config = _fileLogOutputConfigurationFactory.GetConfiguration(_logFilePath);
        _config.SetFlushFrequency(100);
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
        _config.SetFlushFrequency(10);
        var largeEntry = CreateLogEntry(new string('x', 1024 * 1024));
        var rolledLogFile = Path.Combine(_testDirectory, "test.0.log");

        // Act
        await _fileOutput.WriteEntryAsync(largeEntry);
        var secondEntry = CreateLogEntry("Second file entry");
        await Task.Delay(100);
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
        _config.SetRollingInterval(TimeSpan.FromMilliseconds(200));
        var rolledLogFile = Path.Combine(_testDirectory, "test.0.log");
        
        // Act
        await _fileOutput.WriteEntryAsync(CreateLogEntry("First entry"));
        await Task.Delay(350);
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

    [Test]
    public async Task WriteEntryAsync_LargeDataVolume_HandlesMultipleRollovers()
    {
        // Arrange
        _config.SetRollingSizeLimitInMib(2);
        _config.SetFlushFrequency(10);
        const int logEntriesPerFile = 10;
        const int totalFiles = 5;
        const int messageSize = 200 * 1024; // 200 KB per message
        
        var largeMessage = new string('A', messageSize);
        var allRolledFiles = new List<string>();

        // Act
        for (int i = 0; i < logEntriesPerFile * totalFiles; i++)
        {
            await _fileOutput.WriteEntryAsync(CreateLogEntry($"{i}: {largeMessage}"));
            await Task.Delay(20);
        }

        await _fileOutput.DisposeAsync();

        // Assert
        for (int i = 0; i < totalFiles - 1; i++)
        {
            var rolledFile = Path.Combine(_testDirectory, $"test.{i}.log");
            allRolledFiles.Add(rolledFile);
            Assert.That(File.Exists(rolledFile), Is.True, $"Rolled file {rolledFile} should exist");
            
            var fileContent = await File.ReadAllLinesAsync(rolledFile);
            Assert.That(fileContent.Length, Is.GreaterThan(0), $"File {rolledFile} should contain log entries");
        }
        
        // The current file should exist and have entries
        Assert.That(File.Exists(_logFilePath), Is.True, "Current log file should exist");
        var currentFileContent = await File.ReadAllLinesAsync(_logFilePath);
        Assert.That(currentFileContent.Length, Is.GreaterThan(0), "Current log file should contain entries");
    }

    [Test]
    public async Task WriteEntryAsync_BufferLimits_FlushesWhenLimitsReached()
    {
        // Arrange
        const int bufferSizeLimit = 1024; // 1KB
        const int bufferCountLimit = 5;
        
        _config.SetBufferMaxCapacity(bufferCountLimit);
        
        // Act & Assert
        
        // Test buffer count limit
        for (int i = 0; i < bufferCountLimit; i++)
        {
            await _fileOutput.WriteEntryAsync(CreateLogEntry($"Small message {i}"));
        }

        await _fileOutput.DisposeAsync();
        
        // Verify file exists and has content after buffer count limit reached
        Assert.That(File.Exists(_logFilePath), Is.True, "Log file should be created after buffer count limit reached");
        var contentAfterCountLimit = await File.ReadAllLinesAsync(_logFilePath);
        Assert.That(contentAfterCountLimit.Length, Is.EqualTo(bufferCountLimit), 
            "All buffer entries should be written to file");
        
        // Clean up for next part of test
        File.Delete(_logFilePath);
        
        // Re-create output with buffer size limit
        _config = new FileLogOutputConfiguration(_logFilePath);
        _config.SetBufferMaxCapacity(bufferCountLimit);
        
        _fileOutput = new FileLogOutput(_config);
        
        // Test buffer size limit with a large message that exceeds the buffer size limit
        await _fileOutput.WriteEntryAsync(CreateLogEntry(new string('X', bufferSizeLimit + 100)));
        
        await _fileOutput.DisposeAsync();
        
        // Verify file exists and has content after buffer size limit reached
        Assert.That(File.Exists(_logFilePath), Is.True, "Log file should be created after buffer size limit reached");
        var contentAfterSizeLimit = await File.ReadAllLinesAsync(_logFilePath);
        Assert.That(contentAfterSizeLimit.Length, Is.EqualTo(1), "Large entry should be written to file");
    }

    // [Test]
    public async Task WriteEntryAsync_CombinedRollingConditions_RollsFileCorrectly()
    {
        // Arrange
        _config.SetRollingSizeLimitInMib(1);
        _config.SetRollingInterval(TimeSpan.FromMilliseconds(50));
        
        var largeEntry = CreateLogEntry(new string('x', 512 * 1024)); // 500KB entry
        var rolledLogFile1 = Path.Combine(_testDirectory, "test.0.log");
        var rolledLogFile2 = Path.Combine(_testDirectory, "test.1.log");
        
        // Act
        // Write entries that should trigger size-based rolling
        await _fileOutput.WriteEntryAsync(largeEntry);
        await _fileOutput.WriteEntryAsync(largeEntry);
        await _fileOutput.WriteEntryAsync(CreateLogEntry("After size rolling"));
        
        // Wait for time-based rolling to trigger
        await Task.Delay(100);
        
        // Write one more entry that should go to a new file
        await _fileOutput.WriteEntryAsync(CreateLogEntry("After time rolling"));
        
        await _fileOutput.DisposeAsync();
        
        // Assert
        Assert.That(File.Exists(_logFilePath), Is.True, "Current log file should exist");
        Assert.That(File.Exists(rolledLogFile1), Is.True, "First rolled log file should exist");
        Assert.That(File.Exists(rolledLogFile2), Is.True, "Second rolled log file should exist");
        
        var currentFileContent = await File.ReadAllTextAsync(_logFilePath);
        var rolledFile1Content = await File.ReadAllTextAsync(rolledLogFile1);
        var rolledFile2Content = await File.ReadAllTextAsync(rolledLogFile2);
        
        Assert.That(rolledFile1Content.Trim(), Is.EqualTo("After size rolling"), 
            "Current file should contain the last entry after size rolling");
        Assert.That(rolledFile2Content.Trim(), Is.EqualTo("After time rolling"), 
            "Second rolled file should contain entry after time rolling");
    }

    [Test]
    public async Task WriteEntryAsync_HighConcurrencyStressTest_HandlesMultithreadedWrites()
    {
        // Arrange
        const int taskCount = 10;
        const int entriesPerTask = 1000;
        const int totalEntries = taskCount * entriesPerTask;
        
        var allTasks = new List<Task>();
        var messageCounter = new ConcurrentDictionary<string, int>();
        var randomSeed = Environment.TickCount;
        
        _config.SetRollingSizeLimitInMib(10); // Large enough to avoid rolling during test
        _config.SetBufferMaxCapacity(100); // Buffer multiple entries for better performance
        
        // Act
        async Task WritingLogsTask(int localSeed, int taskId)
        {
            // Create a task-local random instance to avoid thread safety issues
            var localRandom = new Random(localSeed);

            for (int i = 0; i < entriesPerTask; i++)
            {
                // Introduce some random delay to simulate real-world conditions
                if (localRandom.Next(100) < 5) // 5% chance to delay
                {
                    await Task.Delay(localRandom.Next(1, 5)).ConfigureAwait(false);
                }

                var message = $"Task-{taskId}-Message-{i}";
                await _fileOutput.WriteEntryAsync(CreateLogEntry(message)).ConfigureAwait(false);
                messageCounter.AddOrUpdate(message, 1, (_, count) => count + 1);
            }
        }

        for (int t = 0; t < taskCount; t++)
        {
            var taskId = t;
            var localSeed = randomSeed + taskId; // Ensure each task has its own random seed
            
            allTasks.Add(Task.Run(() => WritingLogsTask(localSeed, taskId)));
        }
        
        // Wait for all tasks to complete
        await Task.WhenAll(allTasks);
        await _fileOutput.DisposeAsync();
        
        // Assert
        var allLines = await File.ReadAllLinesAsync(_logFilePath);
        
        Assert.That(allLines.Length, Is.EqualTo(totalEntries), 
            "All log entries should be written to the file");
        
        // Verify all messages are written exactly once
        foreach (var line in allLines)
        {
            var trimmedLine = line.Trim();
            Assert.That(messageCounter.ContainsKey(trimmedLine), Is.True, 
                $"Message '{trimmedLine}' should be in the counter dictionary");
            
            messageCounter[trimmedLine]--;
        }
        
        // Ensure all messages were written exactly once
        Assert.That(messageCounter.Values.All(count => count == 0), Is.True, 
            "Each message should appear exactly once in the log file");
    }

    private static ILogEntry CreateLogEntry(string message)
    {
        var mockEntry = new Mock<ILogEntry>();
        mockEntry.Setup(e => e.FinalFormattedMessage).Returns(message);
        return mockEntry.Object;
    }
}