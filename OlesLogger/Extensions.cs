using Microsoft.Extensions.DependencyInjection;

namespace OlesLogger;

public static class Extensions
{
    public static IServiceCollection AddOlesLogger(this IServiceCollection services)
    {
        services.AddScoped<IOlesLogger, OlesLogger>();
        IOlesLogger.AddLogOutput(new ConsoleLogOutput());

        return services;
    }
}