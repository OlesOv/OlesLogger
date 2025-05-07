using BenchmarkDotNet.Running;

namespace Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<LoggerBenchmarks>();
        // var loggerBenchmarks = new LoggerBenchmarks{EntryCount = 100_000};
        // loggerBenchmarks.Setup();
        // loggerBenchmarks.FileLogOutput_Log();
        // loggerBenchmarks.Cleanup();
    }
}
