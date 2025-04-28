using System.Collections.Concurrent;
using System.Text;

namespace OlesLogger.LogOutputs.FileLogOutput;

public sealed class FileLogOutput : ILogOutput, IAsyncDisposable
{
    private readonly FileLogOutputConfiguration _fileLogOutputConfiguration;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ConcurrentQueue<ILogEntry> _buffer = new();
    private int _currentBufferSize = 0;
    private DateTimeOffset _lastFlushTime = DateTimeOffset.UtcNow;
    private readonly Timer? _flushTimer;
    
    public FileLogOutput(FileLogOutputConfiguration fileLogOutputConfiguration)
    {
        _fileLogOutputConfiguration = fileLogOutputConfiguration;
        if (_fileLogOutputConfiguration.BufferTimeoutMs > 0)
        {
            _flushTimer = new Timer(
                (_) => FlushBufferAsync().GetAwaiter().GetResult(), 
                null, 
                _fileLogOutputConfiguration.BufferTimeoutMs, 
                _fileLogOutputConfiguration.BufferTimeoutMs);
        }
    }

    private void EnsureWriterInitialized()
    {
        if (_fileLogOutputConfiguration.Writer != null) return;
        
        _fileLogOutputConfiguration.Writer ??= new StreamWriter(_fileLogOutputConfiguration.CurrentFilePath, true, Encoding.UTF8);;
    }

    private async ValueTask EnsureFileRolledAsync()
    {
        if(_fileLogOutputConfiguration.Disposed || _fileLogOutputConfiguration.Writer == null) return;
        
        if ((_fileLogOutputConfiguration.RollingSizeLimitMib != 0 && _fileLogOutputConfiguration.Writer.BaseStream.Length >= _fileLogOutputConfiguration.RollingSizeLimitMib * 1024 * 1024)
            || (_fileLogOutputConfiguration.RollingInterval != TimeSpan.Zero 
                && DateTimeOffset.UtcNow - _fileLogOutputConfiguration.LastRollDateTimeOffset >= _fileLogOutputConfiguration.RollingInterval))
        {
            await RollFileAsync();
            _fileLogOutputConfiguration.LastRollDateTimeOffset = DateTimeOffset.UtcNow;
        }
    }

    private async ValueTask RollFileAsync()
    {
        if (_fileLogOutputConfiguration.Writer != null)
        {
            await _fileLogOutputConfiguration.Writer.DisposeAsync();
            _fileLogOutputConfiguration.Writer = null;
        }
        _fileLogOutputConfiguration.CurrentFilePath = string.Format(_fileLogOutputConfiguration.FilePathTemplate, _fileLogOutputConfiguration.FileNameBase, _fileLogOutputConfiguration.CurrentFileNumber++);
    }

    public async Task WriteEntryAsync(ILogEntry logEntry)
    {
        ObjectDisposedException.ThrowIf(_fileLogOutputConfiguration.Disposed, nameof(FileLogOutput));
        
        _buffer.Enqueue(logEntry);
        Interlocked.Add(ref _currentBufferSize, logEntry.FinalFormattedMessage.Length * sizeof(char));
        
        bool shouldFlush = _buffer.Count == 1;
        
        if (_buffer.Count >= _fileLogOutputConfiguration.BufferCountLimit)
        {
            shouldFlush = true;
        }
        
        if (_fileLogOutputConfiguration.BufferSizeLimit > 0 && _currentBufferSize >= _fileLogOutputConfiguration.BufferSizeLimit)
        {
            shouldFlush = true;
        }
        
        if (_fileLogOutputConfiguration.BufferTimeoutMs > 0 && _flushTimer == null && 
            (DateTimeOffset.UtcNow - _lastFlushTime).TotalMilliseconds >= _fileLogOutputConfiguration.BufferTimeoutMs)
        {
            shouldFlush = true;
        }
        
        if (shouldFlush)
        {
            await FlushBufferAsync();
        }
    }
    
    private async ValueTask FlushBufferAsync()
    {
        if (_buffer.IsEmpty || _fileLogOutputConfiguration.Disposed) return;
        
        try
        {
            if (!_buffer.IsEmpty)
            {
                await _semaphore.WaitAsync();
                
                if (_buffer.IsEmpty) return;
                
                await EnsureFileRolledAsync();
                EnsureWriterInitialized();
                
                while (_buffer.TryDequeue(out var entry))
                {
                    if (_fileLogOutputConfiguration.Writer != null)
                    {
                        await _fileLogOutputConfiguration.Writer.WriteLineAsync(entry.FinalFormattedMessage);
                    }
                }
                
                if (_fileLogOutputConfiguration.Writer != null)
                {
                    await _fileLogOutputConfiguration.Writer.FlushAsync();
                }
                
                Interlocked.Exchange(ref _currentBufferSize, 0);
                _lastFlushTime = DateTimeOffset.UtcNow;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_fileLogOutputConfiguration.Disposed) return;
        
        await _semaphore.WaitAsync();
        try
        {
            _flushTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            if(_flushTimer != null) await _flushTimer.DisposeAsync();
        
            await FlushBufferAsync();
            if (_fileLogOutputConfiguration.Writer != null)
            {
                await _fileLogOutputConfiguration.Writer.DisposeAsync();
                _fileLogOutputConfiguration.Writer = null;
            }

            _fileLogOutputConfiguration.Disposed = true;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}