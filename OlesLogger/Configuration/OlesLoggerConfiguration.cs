using Microsoft.Extensions.Configuration;
using OlesLogger.LogOutputs;

namespace OlesLogger.Configuration;

public sealed class OlesLoggerConfiguration
{
    public OlesLoggerConfiguration()
    {
    }
    public OlesLoggerConfiguration(string generalFormat)
    {
        GeneralFormat = generalFormat;
    }
    public OlesLoggerConfiguration(IConfiguration configuration)
    {
        configuration.GetSection("OlesLogger").Bind(this);
    }

    public string GeneralFormat { get; set; } = "{FormattedMessage}";
    internal List<ILogOutput> Outputs { get; } = [];
}