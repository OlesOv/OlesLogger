using Microsoft.Extensions.Configuration;
using Moq;
using OlesLogger;

namespace Tests;

public class Tests
{
    private Mock<IConfiguration> _mockConfiguration;
    private Mock<ILogOutput> _mockLogOutput;

    [SetUp]
    public void Setup()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogOutput = new Mock<ILogOutput>();

        // Reset the static Outputs list before each test
        // TODO: fix this static nightmare
        typeof(IOlesLogger).GetField("Outputs", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)?.SetValue(null, new List<ILogOutput>());
        IOlesLogger.AddLogOutput(_mockLogOutput.Object);
    }

    [Test]
    public void Write_SimpleMessage_LogsCorrectly()
    {
        // Arrange
        var message = "Test message";
        var expectedFormattedMessage = "Test message";
        var expectedLogLevel = LogLevel.Information;
        var expectedGeneralFormattedMessage = $"[{expectedLogLevel}] {expectedFormattedMessage}";
        _mockConfiguration.Setup(config => config.GetSection("OlesLogger").GetSection("DefaultFormat").Value).Returns("[{LogLevel}] {FormattedMessage}");

        IOlesLogger logger = new OlesLogger.OlesLogger(_mockConfiguration.Object);

        // Act
        logger.Write(expectedLogLevel, message);

        // Assert
        _mockLogOutput.Verify(
            output => output.WriteEntry(It.Is<LogEntry>(entry =>
                entry.LogLevel == expectedLogLevel &&
                entry.Template == "Test message" &&
                entry.FormattedMessage == expectedFormattedMessage &&
                entry.GeneralFormattedMessage.Equals(expectedGeneralFormattedMessage)
            )),
            Times.Once
        );
    }

    [Test]
    public void Write_MessageWithTemplateAndArguments_LogsCorrectly()
    {
        // Arrange
        var template = "User {Id} logged in with name {Name}";
        var id = 123;
        var name = "John Doe";
        var expectedFormattedMessage = $"User {id} logged in with name {name}";
        var expectedLogLevel = LogLevel.Information;
        _mockConfiguration.Setup(config => config.GetSection("OlesLogger").GetSection("DefaultFormat").Value).Returns("{TimeStamp} - {FormattedMessage}");

        IOlesLogger logger = new OlesLogger.OlesLogger(_mockConfiguration.Object);

        // Act
        logger.Write(expectedLogLevel, template, id, name);

        // Assert
        _mockLogOutput.Verify(
            output => output.WriteEntry(It.Is<LogEntry>(entry =>
                entry.LogLevel == expectedLogLevel &&
                entry.Template == template &&
                entry.FormattedMessage == expectedFormattedMessage &&
                entry.Arguments.ContainsKey("Id") && entry.Arguments["Id"].Equals(id) == true &&
                entry.Arguments.ContainsKey("Name") && entry.Arguments["Name"].Equals(name) == true &&
                entry.GeneralFormattedMessage.Contains(expectedFormattedMessage)
            )),
            Times.Once
        );
    }

    [Test]
    public void Write_MessageWithFewerArgumentsThanPlaceholders_LogsWithNullValues()
    {
        // Arrange
        var template = "User {Id} logged in with name {Name}";
        var id = 123;
        var expectedFormattedMessage = $"User {id} logged in with name {{Name}}";
        var expectedLogLevel = LogLevel.Information;
        _mockConfiguration.Setup(config => config.GetSection("OlesLogger").GetSection("DefaultFormat").Value).Returns("{FormattedMessage}");

        IOlesLogger logger = new OlesLogger.OlesLogger(_mockConfiguration.Object);

        // Act
        logger.Write(expectedLogLevel, template, id);

        // Assert
        _mockLogOutput.Verify(
            output => output.WriteEntry(It.Is<LogEntry>(entry =>
                entry.LogLevel == expectedLogLevel &&
                entry.Template == template &&
                entry.FormattedMessage == expectedFormattedMessage &&
                entry.Arguments.ContainsKey("Id") && entry.Arguments["Id"].Equals(id) == true &&
                entry.Arguments.ContainsKey("Name") && entry.Arguments["Name"] == null
            )),
            Times.Once
        );
    }

    [Test]
    public void Write_NullMessageTemplate_LogsEmptyTemplateAndMessage()
    {
        // Arrange
        var expectedLogLevel = LogLevel.Error;
        _mockConfiguration.Setup(config => config.GetSection("OlesLogger").GetSection("DefaultFormat").Value).Returns("{FormattedMessage}");

        IOlesLogger logger = new OlesLogger.OlesLogger(_mockConfiguration.Object);

        // Act
        logger.Write(expectedLogLevel, null);

        // Assert
        _mockLogOutput.Verify(
            output => output.WriteEntry(It.Is<LogEntry>(entry =>
                entry.LogLevel == expectedLogLevel &&
                entry.Template == "" &&
                entry.FormattedMessage == ""
            )),
            Times.Once
        );
    }

    [Test]
    public void LogLevelMethods_CallWriteWithCorrectLevel()
    {
        // Arrange
        _mockConfiguration.Setup(config => config.GetSection("OlesLogger").GetSection("DefaultFormat").Value).Returns("");

        IOlesLogger logger = new OlesLogger.OlesLogger(_mockConfiguration.Object);
        var message = "Test message";

        // Act
        logger.Critical(message);
        logger.Error(message);
        logger.Warning(message);
        logger.Information(message);
        logger.Verbose(message);

        // Assert
        _mockLogOutput.Verify(output => output.WriteEntry(It.Is<LogEntry>(entry => entry.LogLevel == LogLevel.Critical && entry.Template == message)), Times.Once);
        _mockLogOutput.Verify(output => output.WriteEntry(It.Is<LogEntry>(entry => entry.LogLevel == LogLevel.Error && entry.Template == message)), Times.Once);
        _mockLogOutput.Verify(output => output.WriteEntry(It.Is<LogEntry>(entry => entry.LogLevel == LogLevel.Warning && entry.Template == message)), Times.Once);
        _mockLogOutput.Verify(output => output.WriteEntry(It.Is<LogEntry>(entry => entry.LogLevel == LogLevel.Information && entry.Template == message)), Times.Once);
        _mockLogOutput.Verify(output => output.WriteEntry(It.Is<LogEntry>(entry => entry.LogLevel == LogLevel.Verbose && entry.Template == message)), Times.Once);
    }

    [Test]
    public void Write_UsesDefaultFormatFromConfiguration()
    {
        // Arrange
        var message = "Formatted message content";
        var defaultFormat = "{LogLevel}: {FormattedMessage}";
        _mockConfiguration.Setup(config => config.GetSection("OlesLogger").GetSection("DefaultFormat").Value).Returns(defaultFormat);

        IOlesLogger logger = new OlesLogger.OlesLogger(_mockConfiguration.Object);

        // Act
        logger.Write(LogLevel.Verbose, message);

        // Assert
        _mockLogOutput.Verify(output => output.WriteEntry(It.Is<LogEntry>(entry =>
            entry.GeneralFormattedMessage == $"Verbose: {message}"
        )), Times.Once);
    }
}