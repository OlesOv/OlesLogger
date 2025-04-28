using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OlesLogger.LogOutputs;

namespace OlesLogger.Configuration;

public sealed class OlesLoggerConfiguration
{
    public OlesLoggerConfiguration()
    {
    }
    public OlesLoggerConfiguration(string finalFormatTemplate)
    {
        FinalFormatTemplate = finalFormatTemplate;
    }
    public OlesLoggerConfiguration(IConfiguration configuration)
    {
        configuration.GetSection("OlesLogger").Bind(this);
    }

    public string FinalFormatTemplate { get; set; } = "{FormattedMessage}";
    internal List<ILogOutput> Outputs { get; } = [];

    internal ILoggingBuilder LoggingBuilder { get; set; } = null!;
}