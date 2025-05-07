using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using OlesLogger;
using OlesLogger.Configuration;
using OlesLogger.LogOutputs.ConsoleLogOutput;
using OlesLogger.LogOutputs.FileLogOutput;

namespace Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class LoggerBenchmarks
{
    private static readonly string TempDir = Path.Combine(Path.GetTempPath(), "loggerBenchmark");
    private static readonly string TempFilePath = Path.Combine(TempDir, "benchmark_logs.txt");
    private static readonly string ConsoleAndFileLoggerFilePath = Path.Combine(TempDir, "benchmark_logs_1.txt");
    
    private IOlesLogger _fileLogger = null!;
    private IOlesLogger _consoleLogger = null!;
    private IOlesLogger _consoleAndFileLogger = null!;

    [Params(100, 1_000, 10_000, 100_000, 1_000_000/*, 100_000_000*/)]
    public int EntryCount { get; set; }
    
    [GlobalSetup]
    public void Setup()
    {
        // Clean up previous file if exists
        ClearTempLogs();
        var fileLoggerConfig = new OlesLoggerConfiguration()
            .AddFileOutput(TempFilePath, configuration => configuration
                .SetFlushFrequency(500)
                .SetBufferMaxCapacity(1000)
                .SetRollingSizeLimitInMib(100));
        _fileLogger = new OlesLogger.OlesLogger(fileLoggerConfig);
        
        
        var consoleLoggerConfig = new OlesLoggerConfiguration().AddConsoleOutput();
        _consoleLogger = new OlesLogger.OlesLogger(consoleLoggerConfig);
        
        
        // Clean up previous file if exists
        if (File.Exists(ConsoleAndFileLoggerFilePath))
        {
            File.Delete(ConsoleAndFileLoggerFilePath);
        }
        var consoleAndFileLoggerConfig = new OlesLoggerConfiguration()
            .AddFileOutput(ConsoleAndFileLoggerFilePath, configuration => configuration
                .SetFlushFrequency(100)
                .SetRollingSizeLimitInMib(100))
            .AddConsoleOutput();
        _consoleAndFileLogger = new OlesLogger.OlesLogger(consoleAndFileLoggerConfig);
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        // ClearTempLogs();

        _fileLogger.DisposeAsync().GetAwaiter().GetResult();
        _consoleLogger.DisposeAsync().GetAwaiter().GetResult();
        _consoleAndFileLogger.DisposeAsync().GetAwaiter().GetResult();
    }
    
    [Benchmark]
    public void FileLogOutput_Log()
    {
        for (int i = 0; i < EntryCount; i++)
        {
            _fileLogger.Write(LogLevels.Information, "Test log message {messageNumber}", i);
        }
    }
    
    [Benchmark]
    public void ConsoleLogOutput_Log()
    {
        for (int i = 0; i < EntryCount; i++)
        {
            _consoleLogger.Write(LogLevels.Information, "Test log message {messageNumber}", i);
        }
    }

    [IterationSetup]
    public void IterationSetup()
    {
        using var redirectedConsoleOutput = new StringWriter();
        Console.SetOut(new StringWriter());
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        var standardOutput = new StreamWriter(Console.OpenStandardOutput());
        standardOutput.AutoFlush = true;
        Console.SetOut(standardOutput);
    }

    private void ClearTempLogs()
    {
        Directory.Delete(TempDir, true);
    }
}
