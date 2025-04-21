using System.Collections.Concurrent;
using System.Text;

namespace OlesLogger.LogOutputs.FileLogOutput;

public sealed class FileLogOutput : ILogOutput, IAsyncDisposable
{
    private readonly FileLogOutputConfiguration _config;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ConcurrentQueue<ILogEntry> _buffer = new();
    private int _currentBufferSize = 0;
    private DateTimeOffset _lastFlushTime = DateTimeOffset.UtcNow;
    private readonly Timer? _flushTimer;
    
    public FileLogOutput(FileLogOutputConfiguration config)
    {
        _config = config;
        if (_config.BufferTimeoutMs > 0)
        {
            _flushTimer = new Timer(
                async void (_) => await FlushBufferAsync(false), 
                null, 
                _config.BufferTimeoutMs, 
                _config.BufferTimeoutMs);
        }
    }

    private void EnsureWriterInitialized()
    {
        if (_config.Writer != null) return;
        
        _config.Writer ??= new StreamWriter(_config.CurrentFilePath, true, Encoding.UTF8);;
    }

    private async ValueTask EnsureFileRolledAsync()
    {
        if(_config.Disposed || _config.Writer == null) return;
        
        if ((_config.RollingSizeLimitMib != 0 && _config.Writer.BaseStream.Length >= _config.RollingSizeLimitMib * 1024 * 1024)
            || (_config.RollingInterval != TimeSpan.Zero 
                && DateTimeOffset.UtcNow - _config.LastRollDateTimeOffset >= _config.RollingInterval))
        {
            await RollFileAsync();
            _config.LastRollDateTimeOffset = DateTimeOffset.UtcNow;
        }
    }

    private async ValueTask RollFileAsync()
    {
        if (_config.Writer != null)
        {
            await _config.Writer.DisposeAsync();
            _config.Writer = null;
        }
        _config.CurrentFilePath = string.Format(_config.FilePathTemplate, _config.FileNameBase, _config.CurrentFileNumber++);
    }

    public async Task WriteEntryAsync(ILogEntry entry)
    {
        ObjectDisposedException.ThrowIf(_config.Disposed, nameof(FileLogOutput));
        
        _buffer.Enqueue(entry);
        Interlocked.Add(ref _currentBufferSize, entry.GeneralFormattedMessage.Length * sizeof(char));
        
        bool shouldFlush = _buffer.Count == 1;
        
        // Check buffer count
        if (_buffer.Count >= _config.BufferCountLimit)
        {
            shouldFlush = true;
        }
        
        // Check buffer size limit
        if (_config.BufferSizeLimit > 0 && _currentBufferSize >= _config.BufferSizeLimit)
        {
            shouldFlush = true;
        }
        
        // Check timeout (if timer is not being used)
        if (_config.BufferTimeoutMs > 0 && _flushTimer == null && 
            (DateTimeOffset.UtcNow - _lastFlushTime).TotalMilliseconds >= _config.BufferTimeoutMs)
        {
            shouldFlush = true;
        }
        
        if (shouldFlush)
        {
            await FlushBufferAsync(false);
        }
    }
    
    private async ValueTask FlushBufferAsync(bool forceLock)
    {
        if (_buffer.IsEmpty || _config.Disposed) return;
        
        bool lockTaken = false;
        try
        {
            if (forceLock || !_buffer.IsEmpty)
            {
                await _semaphore.WaitAsync();
                lockTaken = true;
                
                if (_buffer.IsEmpty) return;
                
                await EnsureFileRolledAsync();
                EnsureWriterInitialized();
                
                while (_buffer.TryDequeue(out var entry))
                {
                    if (_config.Writer != null)
                    {
                        await _config.Writer.WriteLineAsync(entry.GeneralFormattedMessage);
                    }
                }
                
                if (_config.Writer != null)
                {
                    await _config.Writer.FlushAsync();
                }
                
                Interlocked.Exchange(ref _currentBufferSize, 0);
                _lastFlushTime = DateTimeOffset.UtcNow;
            }
        }
        finally
        {
            if (lockTaken)
            {
                _semaphore.Release();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_config.Disposed) return;
        
        await _semaphore.WaitAsync();
        try
        {
            _flushTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            if(_flushTimer != null) await _flushTimer.DisposeAsync();
        
            await FlushBufferAsync(true);
            if (_config.Writer != null)
            {
                await _config.Writer.DisposeAsync();
                _config.Writer = null;
            }

            _config.Disposed = true;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}