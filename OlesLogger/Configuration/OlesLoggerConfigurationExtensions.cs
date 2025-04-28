using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using OlesLogger.LogOutputs;

namespace OlesLogger.Configuration;

public static class OlesLoggerConfigurationExtensions
{
    public static ILoggingBuilder AddOlesLogger(this ILoggingBuilder loggingBuilder, OlesLoggerConfiguration configuration)
    {
        configuration.LoggingBuilder = loggingBuilder;
        loggingBuilder.Services.TryAdd(ServiceDescriptor.Scoped<IOlesLogger, OlesLogger>(_ => new OlesLogger(configuration)));

        return loggingBuilder;
    }
    public static OlesLoggerConfiguration AddLogOutput(this OlesLoggerConfiguration configuration, ILogOutput logOutput)
    {
        configuration.Outputs.Add(logOutput);
        return configuration;
    }
}