using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using OlesLogger.LogOutputs;

namespace OlesLogger.Configuration;

public static class LoggingBuilderExtensions
{
    public static ILoggingBuilder AddOlesLogger(this ILoggingBuilder services, OlesLoggerConfiguration configuration)
    {
        services.Services.TryAdd(ServiceDescriptor.Scoped<IOlesLogger, OlesLogger>(_ => new OlesLogger(configuration)));

        return services;
    }
    public static OlesLoggerConfiguration AddLogOutput(this OlesLoggerConfiguration configuration, ILogOutput logOutput)
    {
        configuration.Outputs.Add(logOutput);
        return configuration;
    }
}