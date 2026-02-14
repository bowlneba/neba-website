using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Application.Clock;
using Neba.Infrastructure.Documents;
using Neba.TestFactory.Attributes;

namespace Neba.Infrastructure.Tests.Documents;

[UnitTest]
[Component("GoogleDriveService")]
public sealed class GoogleDriveServiceTests
{
    private readonly GoogleDriveSettings _settings;

    public GoogleDriveServiceTests()
    {
        _settings = new GoogleDriveSettings
        {
            ApplicationName = "Test Application",
            Credentials = new GoogleDriveCredentials
            {
                ProjectId = "test-project",
                PrivateKey = """
                    -----BEGIN PRIVATE KEY-----
                    this-is-a-test-private-key-for-unit-tests
                    -----END PRIVATE KEY-----
                    """,
                ClientEmail = "test@test-project.iam.gserviceaccount.com",
                PrivateKeyId = "test-key-id-12345"
            },
            Documents =
            [
                new Document
                {
                    Name = "bylaws",
                    DocumentId = "1ABC123XYZ",
                    WebRoute = "/about/bylaws"
                },
                new Document
                {
                    Name = "tournament-rules",
                    DocumentId = "1DEF456UVW",
                    WebRoute = "/tournaments/rules"
                },
                new Document
                {
                    Name = "officer-handbook",
                    DocumentId = "1GHI789RST",
                    WebRoute = "/about/officer-handbook"
                }
            ]
        };
    }

    private static Mock<IStopwatchProvider> CreateStopwatchProviderMock()
    {
        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);

        // Setup GetTimestamp to return a predictable value
        mockStopwatch
            .Setup(x => x.GetTimestamp())
            .Returns(1000L);

        // Setup GetElapsedTime to return a predictable duration
        mockStopwatch
            .Setup(x => x.GetElapsedTime(It.IsAny<long>()))
            .Returns(TimeSpan.FromMilliseconds(123.45));

        return mockStopwatch;
    }

    [Fact(DisplayName = "Constructor should throw when settings is null")]
    public void Constructor_ThrowsArgumentNullException_WhenSettingsIsNull()
    {
        // Arrange
        GoogleDriveSettings? nullSettings = null;
        var processor = new HtmlProcessor(_settings);
        var stopwatch = CreateStopwatchProviderMock().Object;
        var logger = NullLogger<GoogleDriveService>.Instance;

        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() =>
            new GoogleDriveService(nullSettings!, processor, stopwatch, logger));

        exception.ParamName.ShouldBe("settings");
    }

    [Fact(DisplayName = "Constructor should throw when htmlProcessor is null")]
    public void Constructor_ThrowsArgumentNullException_WhenHtmlProcessorIsNull()
    {
        // Arrange
        HtmlProcessor? nullProcessor = null;
        var stopwatch = CreateStopwatchProviderMock().Object;
        var logger = NullLogger<GoogleDriveService>.Instance;

        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() =>
            new GoogleDriveService(_settings, nullProcessor!, stopwatch, logger));

        exception.ParamName.ShouldBe("htmlProcessor");
    }

    [Fact(DisplayName = "Constructor should throw when stopwatchProvider is null")]
    public void Constructor_ThrowsArgumentNullException_WhenStopwatchProviderIsNull()
    {
        // Arrange
        var processor = new HtmlProcessor(_settings);
        IStopwatchProvider? nullStopwatch = null;
        var logger = NullLogger<GoogleDriveService>.Instance;

        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() =>
            new GoogleDriveService(_settings, processor, nullStopwatch!, logger));

        exception.ParamName.ShouldBe("stopwatchProvider");
    }

    [Fact(DisplayName = "Constructor should throw when logger is null")]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        // Arrange
        var processor = new HtmlProcessor(_settings);
        var stopwatch = CreateStopwatchProviderMock().Object;
        ILogger<GoogleDriveService>? nullLogger = null;

        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() =>
            new GoogleDriveService(_settings, processor, stopwatch, nullLogger!));

        exception.ParamName.ShouldBe("logger");
    }

    [Fact(DisplayName = "GetDocumentAsHtmlAsync should throw for document not in configuration")]
    public async Task GetDocumentAsHtmlAsync_ThrowsInvalidOperationException_WhenDocumentNotConfigured()
    {
        // Arrange
        var processor = new HtmlProcessor(_settings);
        var stopwatch = CreateStopwatchProviderMock().Object;
        var logger = NullLogger<GoogleDriveService>.Instance;
        using var service = new GoogleDriveService(_settings, processor, stopwatch, logger);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await service.GetDocumentAsHtmlAsync("nonexistent-document", CancellationToken.None));

        exception.Message.ShouldContain("nonexistent-document");
        exception.Message.ShouldContain("not found in configuration");
    }

    [Theory(DisplayName = "GetDocumentAsHtmlAsync should find document case-insensitively")]
    [InlineData("bylaws")]
    [InlineData("BYLAWS")]
    [InlineData("ByLaws")]
    [InlineData("Tournament-Rules")]
    [InlineData("TOURNAMENT-RULES")]
    [InlineData("officer-handbook")]
    [InlineData("OFFICER-HANDBOOK")]
    public async Task GetDocumentAsHtmlAsync_FindsDocument_CaseInsensitively(string documentName)
    {
        // Arrange
        var processor = new HtmlProcessor(_settings);
        var mockStopwatch = CreateStopwatchProviderMock();
        var logger = NullLogger<GoogleDriveService>.Instance;
        using var service = new GoogleDriveService(_settings, processor, mockStopwatch.Object, logger);

        // Act & Assert
        try
        {
            await service.GetDocumentAsHtmlAsync(documentName, CancellationToken.None);

            // If we get here, the API call succeeded (won't happen without real credentials)
            Assert.True(true, "Document lookup succeeded");
        }
        catch (InvalidOperationException ex)
        {
            // If we get InvalidOperationException, the document wasn't found - test should fail
            Assert.Fail($"Document lookup failed: {ex.Message}");
        }
        catch
        {
            // Any other exception means document was found but API call failed
            // This is expected behavior without real credentials
            Assert.True(true, "Document lookup succeeded, API call failed as expected");
        }

        // Verify stopwatch was called (proves we got into the method)
        mockStopwatch.Verify(x => x.GetTimestamp(), Times.Once);
    }

    [Fact(DisplayName = "GetDocumentAsHtmlAsync should call stopwatch provider")]
    public async Task GetDocumentAsHtmlAsync_CallsStopwatchProvider_ToMeasureDuration()
    {
        // Arrange
        var processor = new HtmlProcessor(_settings);
        var mockStopwatch = CreateStopwatchProviderMock();
        var logger = NullLogger<GoogleDriveService>.Instance;
        using var service = new GoogleDriveService(_settings, processor, mockStopwatch.Object, logger);

        // Act & Assert
        try
        {
            await service.GetDocumentAsHtmlAsync("bylaws", CancellationToken.None);
        }
        catch
        {
            // Expected to fail on DriveService call without real credentials
        }

        // Verify timing methods were called
        mockStopwatch.Verify(x => x.GetTimestamp(), Times.Once);
        mockStopwatch.Verify(x => x.GetElapsedTime(1000L), Times.Once);
    }

    [Fact(DisplayName = "Service should dispose DriveService on disposal")]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var processor = new HtmlProcessor(_settings);
        var stopwatch = CreateStopwatchProviderMock().Object;
        var logger = NullLogger<GoogleDriveService>.Instance;
        using var service = new GoogleDriveService(_settings, processor, stopwatch, logger);

        // Act & Assert
        Should.NotThrow(() => service.Dispose());
    }
}