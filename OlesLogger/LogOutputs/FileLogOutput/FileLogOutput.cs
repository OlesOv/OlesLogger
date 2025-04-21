using System.Text;

namespace OlesLogger.LogOutputs.FileLogOutput;

public sealed class FileLogOutput(FileLogOutputConfiguration config) : ILogOutput, IAsyncDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private async ValueTask EnsureWriterInitializedAsync()
    {
        if (config.Writer != null) return;
        
        await _semaphore.WaitAsync();
        try
        {
            config.Writer ??= new StreamWriter(config.CurrentFilePath, true, Encoding.UTF8);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async ValueTask EnsureFileRolledAsync()
    {
        if(config.Disposed || config.Writer == null) return;
        await _semaphore.WaitAsync();
        try
        {
            if ((config.RollingSizeLimitMib != 0 && config.Writer.BaseStream.Length >= config.RollingSizeLimitMib * 1024 * 1024)
                || (config.RollingInterval != TimeSpan.Zero 
                    && DateTimeOffset.UtcNow - config.LastRollDateTimeOffset >= config.RollingInterval))
            {
                await RollFileAsync();
                config.LastRollDateTimeOffset = DateTimeOffset.UtcNow;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async ValueTask RollFileAsync()
    {
        if (config.Writer != null)
        {
            await config.Writer.DisposeAsync();
            config.Writer = null;
        }
        config.CurrentFilePath = string.Format(config.FilePathTemplate, config.FileNameBase, config.CurrentFileNumber++);
    }

    public async Task WriteEntryAsync(ILogEntry entry)
    {
        ObjectDisposedException.ThrowIf(config.Disposed, nameof(FileLogOutput));
        
        await EnsureFileRolledAsync();
        await EnsureWriterInitializedAsync();
        await _semaphore.WaitAsync();
        try
        {
            if (config.Writer != null)
            {
                await config.Writer.WriteLineAsync(entry.GeneralFormattedMessage);
                await config.Writer.FlushAsync();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (config.Disposed) return;
        await _semaphore.WaitAsync();
        try
        {
            if (config.Writer != null)
            {
                await config.Writer.DisposeAsync();
                config.Writer = null;
            }

            config.Disposed = true;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}