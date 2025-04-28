using Moq;
using OlesLogger;
using OlesLogger.Configuration;
using OlesLogger.LogOutputs;

namespace Tests;

public class Tests
{
    private OlesLoggerConfiguration _loggerConfiguration;
    private Mock<ILogOutput> _mockLogOutput;

    [SetUp]
    public void Setup()
    {
        _loggerConfiguration = new OlesLoggerConfiguration("[{LogLevel}] {FormattedMessage}");
        _mockLogOutput = new Mock<ILogOutput>();

        _loggerConfiguration.AddLogOutput(_mockLogOutput.Object);
    }

    [Test]
    public void Write_SimpleMessage_LogsCorrectly()
    {
        // Arrange
        var message = "Test message";
        var expectedFormattedMessage = "Test message";
        var expectedLogLevel = LogLevels.Information;
        var expectedFinalFormatMessage = $"[{expectedLogLevel}] {expectedFormattedMessage}";

        IOlesLogger logger = new OlesLogger.OlesLogger(_loggerConfiguration);

        // Act
        logger.Write(expectedLogLevel, message);

        // Assert
        _mockLogOutput.Verify(
            output => output.WriteEntryAsync(It.Is<LogEntry>(entry =>
                entry.LogLevel == expectedLogLevel &&
                entry.MessageTemplate == "Test message" &&
                entry.FormattedMessage == expectedFormattedMessage &&
                entry.FinalFormattedMessage.Equals(expectedFinalFormatMessage)
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
        var expectedLogLevel = LogLevels.Information;

        _loggerConfiguration.FinalFormatTemplate = "{TimeStamp} - {FormattedMessage}";

        IOlesLogger logger = new OlesLogger.OlesLogger(_loggerConfiguration);

        // Act
        logger.Write(expectedLogLevel, template, id, name);

        // Assert
        _mockLogOutput.Verify(
            output => output.WriteEntryAsync(It.Is<LogEntry>(entry =>
                entry.LogLevel == expectedLogLevel &&
                entry.MessageTemplate == template &&
                entry.FormattedMessage == expectedFormattedMessage &&
                entry.Arguments.Any(a => a.key == "Id" && string.Equals(a.value!.ToString(), id.ToString(),
                    StringComparison.InvariantCultureIgnoreCase)) &&
                entry.Arguments.Any(a => a.key == "Name" && string.Equals(a.value!.ToString(), name,
                    StringComparison.InvariantCultureIgnoreCase)) &&
                entry.FinalFormattedMessage.Contains(expectedFormattedMessage)
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
        var expectedLogLevel = LogLevels.Information;
        _loggerConfiguration.FinalFormatTemplate = "{FormattedMessage}";

        IOlesLogger logger = new OlesLogger.OlesLogger(_loggerConfiguration);

        // Act
        logger.Write(expectedLogLevel, template, id);

        // Assert
        _mockLogOutput.Verify(
            output => output.WriteEntryAsync(It.Is<LogEntry>(entry =>
                entry.LogLevel == expectedLogLevel &&
                entry.MessageTemplate == template &&
                entry.FormattedMessage == expectedFormattedMessage &&
                entry.Arguments.Any(a => a.key == "Id" && string.Equals(a.value!.ToString(), id.ToString(),
                    StringComparison.InvariantCultureIgnoreCase)) &&
                entry.Arguments.Any(a =>
                    a.key == "Name" && Equals(a.value, null))
            )),
            Times.Once
        );
    }

    [Test]
    public void Write_MessageWithMultipleArgumentsWithSamename_LogsCorrectly()
    {
        // Arrange
        var template = "User {Id} logged in with name {Name}. Approved by admin {Name}";
        var id = 123;
        var name = "John Doe";
        var adminName = "Jane Doe";
        var expectedFormattedMessage = $"User {id} logged in with name {name}. Approved by admin {adminName}";
        var expectedLogLevel = LogLevels.Information;
        _loggerConfiguration.FinalFormatTemplate = "{FormattedMessage}";

        IOlesLogger logger = new OlesLogger.OlesLogger(_loggerConfiguration);

        // Act
        logger.Write(expectedLogLevel, template, id, name, adminName);

        // Assert
        _mockLogOutput.Verify(
            output => output.WriteEntryAsync(It.Is<LogEntry>(entry =>
                entry.LogLevel == expectedLogLevel &&
                entry.MessageTemplate == template &&
                entry.FormattedMessage == expectedFormattedMessage &&
                entry.Arguments.Any(a => a.key == "Id" && string.Equals(a.value!.ToString(), id.ToString(),
                    StringComparison.InvariantCultureIgnoreCase)) &&
                entry.Arguments.Any(a =>
                    a.key == "Name" && string.Equals(a.value!.ToString(), name,
                        StringComparison.InvariantCultureIgnoreCase)) &&
                entry.Arguments.Any(a =>
                    a.key == "Name" && string.Equals(a.value!.ToString(), adminName,
                        StringComparison.InvariantCultureIgnoreCase))
            )),
            Times.Once
        );
    }

    [Test]
    public void Write_NullMessageTemplate_LogsEmptyTemplateAndMessage()
    {
        // Arrange
        var expectedLogLevel = LogLevels.Error;
        _loggerConfiguration.FinalFormatTemplate = "{FormattedMessage}";

        IOlesLogger logger = new OlesLogger.OlesLogger(_loggerConfiguration);

        // Act
        logger.Write(expectedLogLevel, null);

        // Assert
        _mockLogOutput.Verify(
            output => output.WriteEntryAsync(It.Is<LogEntry>(entry =>
                entry.LogLevel == expectedLogLevel &&
                entry.MessageTemplate == "" &&
                entry.FormattedMessage == ""
            )),
            Times.Once
        );
    }

    [Test]
    public void LogLevelMethods_CallWriteWithCorrectLevel()
    {
        // Arrange
        _loggerConfiguration.FinalFormatTemplate = "";

        IOlesLogger logger = new OlesLogger.OlesLogger(_loggerConfiguration);
        var message = "Test message";

        // Act
        logger.Critical(message);
        logger.Error(message);
        logger.Warning(message);
        logger.Information(message);
        logger.Verbose(message);

        // Assert
        _mockLogOutput.Verify(
            output => output.WriteEntryAsync(It.Is<LogEntry>(entry =>
                entry.LogLevel == LogLevels.Critical && entry.MessageTemplate == message)), Times.Once);
        _mockLogOutput.Verify(
            output => output.WriteEntryAsync(It.Is<LogEntry>(entry =>
                entry.LogLevel == LogLevels.Error && entry.MessageTemplate == message)), Times.Once);
        _mockLogOutput.Verify(
            output => output.WriteEntryAsync(It.Is<LogEntry>(entry =>
                entry.LogLevel == LogLevels.Warning && entry.MessageTemplate == message)), Times.Once);
        _mockLogOutput.Verify(
            output => output.WriteEntryAsync(It.Is<LogEntry>(entry =>
                entry.LogLevel == LogLevels.Information && entry.MessageTemplate == message)), Times.Once);
        _mockLogOutput.Verify(
            output => output.WriteEntryAsync(It.Is<LogEntry>(entry =>
                entry.LogLevel == LogLevels.Verbose && entry.MessageTemplate == message)), Times.Once);
    }

    [Test]
    public void Write_UsesDefaultFormatFromConfiguration()
    {
        // Arrange
        var message = "Formatted message content";
        var defaultFormat = "{LogLevel}: {FormattedMessage}";

        _loggerConfiguration.FinalFormatTemplate = defaultFormat;

        IOlesLogger logger = new OlesLogger.OlesLogger(_loggerConfiguration);

        // Act
        logger.Write(LogLevels.Verbose, message);

        // Assert
        _mockLogOutput.Verify(output => output.WriteEntryAsync(It.Is<LogEntry>(entry =>
            entry.FinalFormattedMessage == $"Verbose: {message}"
        )), Times.Once);
    }
}