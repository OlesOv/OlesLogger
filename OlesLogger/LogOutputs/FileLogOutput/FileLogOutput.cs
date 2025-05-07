using System.Collections.Concurrent;
using System.Text;

namespace OlesLogger.LogOutputs.FileLogOutput;

public sealed class FileLogOutput : ILogOutput
{
    private readonly FileLogOutputConfiguration _fileLogOutputConfiguration;
    private readonly PeriodicTimer _consumerTimer;

    public FileLogOutput(FileLogOutputConfiguration fileLogOutputConfiguration)
    {
        _fileLogOutputConfiguration = fileLogOutputConfiguration;
        _consumerTimer = new(TimeSpan.FromMilliseconds(_fileLogOutputConfiguration.FlushFrequencyMs));
        _ = StartAsynchronousLoop();
    }


    public ValueTask WriteEntryAsync(ILogEntry logEntry)
    {
        ObjectDisposedException.ThrowIf(_fileLogOutputConfiguration.Disposed, nameof(FileLogOutput));

        _fileLogOutputConfiguration.LogEntryQueue.TryAdd(logEntry);
        return ValueTask.CompletedTask;
    }
    
    private async ValueTask StartAsynchronousLoop()
    {
        while (await _consumerTimer.WaitForNextTickAsync())
        {
            await Consume();
        }
    }
    
    private async ValueTask Consume()
    {
        var currentDateTime = DateTimeOffset.UtcNow;
        if (_fileLogOutputConfiguration.LogFileLastRolled == default)
            _fileLogOutputConfiguration.LogFileLastRolled = currentDateTime;

        // To create file first
        bool shouldFlush = _fileLogOutputConfiguration.LogEntryQueue.Count == 1;

        if (_fileLogOutputConfiguration.LogEntryQueue.Count >= _fileLogOutputConfiguration.BufferCountLimit)
        {
            shouldFlush = true;
        }

        if (shouldFlush)
        {
            await FlushBufferAsync();
        }
    }
    
    private void EnsureWriterInitialized()
    {
        if (_fileLogOutputConfiguration.Writer != null) return;

        _fileLogOutputConfiguration.Writer ??=
            new StreamWriter(_fileLogOutputConfiguration.CurrentLogFilePath, true, Encoding.UTF8);
        
    }

    private async ValueTask EnsureFileRolledAsync()
    {
        if (_fileLogOutputConfiguration.Disposed || _fileLogOutputConfiguration.Writer == null) return;
        var currentDateTime = DateTimeOffset.UtcNow;

        bool shouldRollBySize = _fileLogOutputConfiguration.RollingSizeLimitMib != 0 &&
                 _fileLogOutputConfiguration.Writer.BaseStream.Length >=
                 _fileLogOutputConfiguration.RollingSizeLimitMib * 1024 * 1024;
        var shouldRollByTime = _fileLogOutputConfiguration.RollingInterval != TimeSpan.Zero
                               && currentDateTime - _fileLogOutputConfiguration.LogFileLastRolled >=
                               _fileLogOutputConfiguration.RollingInterval;
        
        if (shouldRollBySize || shouldRollByTime)
        {
            await RollFileAsync();
            _fileLogOutputConfiguration.LogFileLastRolled = currentDateTime;
            EnsureWriterInitialized();
        }
    }

    private async ValueTask RollFileAsync()
    {
        if (_fileLogOutputConfiguration.Writer != null)
        {
            await _fileLogOutputConfiguration.Writer.DisposeAsync();
            _fileLogOutputConfiguration.Writer = null;
        }

        _fileLogOutputConfiguration.CurrentLogFilePath = string.Format(_fileLogOutputConfiguration.LogFilePathTemplate,
            _fileLogOutputConfiguration.LogFileNameBase, _fileLogOutputConfiguration.CurrentLogFileNumber++);
    }

    private async ValueTask FlushBufferAsync()
    {
        if (_fileLogOutputConfiguration.LogEntryQueue.IsCompleted || _fileLogOutputConfiguration.Disposed) return;
        
        while (_fileLogOutputConfiguration.LogEntryQueue.TryTake(out var entry))
        {
            EnsureWriterInitialized();
            await EnsureFileRolledAsync();
            if (_fileLogOutputConfiguration.Writer != null)
            {
                await _fileLogOutputConfiguration.Writer.WriteLineAsync(entry.FinalFormattedMessage);
            }
        }
        
        if (_fileLogOutputConfiguration.Writer != null)
        {
            await _fileLogOutputConfiguration.Writer.FlushAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_fileLogOutputConfiguration.Disposed) return;

        _consumerTimer.Dispose();

        await Consume();
        if (_fileLogOutputConfiguration.Writer != null)
        {
            await _fileLogOutputConfiguration.Writer.DisposeAsync();
            _fileLogOutputConfiguration.Writer = null;
        }

        _fileLogOutputConfiguration.Disposed = true;
    }
}