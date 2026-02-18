using Microsoft.Extensions.Logging.Abstractions;

using Neba.Application.Clock;
using Neba.Application.Documents;
using Neba.Application.Documents.SyncDocument;
using Neba.Application.Storage;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Documents;

namespace Neba.Application.Tests.Documents.SyncDocument;

[UnitTest]
[Component("Documents")]
public sealed class SyncDocumentToStorageJobHandlerTests
{
    private readonly Mock<IDocumentsService> _documentsServiceMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly Mock<IStopwatchProvider> _stopwatchProviderMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;

    private readonly SyncDocumentToStorageJobHandler _handler;

    public SyncDocumentToStorageJobHandlerTests()
    {
        _documentsServiceMock = new Mock<IDocumentsService>(MockBehavior.Strict);
        _fileStorageServiceMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        _stopwatchProviderMock = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        _dateTimeProviderMock = new Mock<IDateTimeProvider>(MockBehavior.Strict);

        _stopwatchProviderMock
            .Setup(s => s.GetTimestamp())
            .Returns(1000L);

        _stopwatchProviderMock
            .Setup(s => s.GetElapsedTime(It.IsAny<long>()))
            .Returns(TimeSpan.FromMilliseconds(50));

        _handler = new SyncDocumentToStorageJobHandler(
            _documentsServiceMock.Object,
            _fileStorageServiceMock.Object,
            _stopwatchProviderMock.Object,
            _dateTimeProviderMock.Object,
            NullLogger<SyncDocumentToStorageJobHandler>.Instance);
    }

    [Fact(DisplayName = "Should retrieve document and upload to storage on success")]
    public async Task ExecuteAsync_ShouldRetrieveAndUpload_WhenDocumentExists()
    {
        // Arrange
        var modifiedAt = new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero);
        var document = DocumentDtoFactory.Create(modifiedAt: modifiedAt);
        var cachedAt = new DateTimeOffset(2026, 2, 17, 7, 0, 0, TimeSpan.Zero);
        var job = new SyncDocumentToStorageJob
        {
            DocumentName = document.Name,
            TriggeredBy = "scheduled"
        };

        _documentsServiceMock
            .Setup(s => s.GetDocumentAsHtmlAsync(job.DocumentName, TestContext.Current.CancellationToken))
            .ReturnsAsync(document);

        _dateTimeProviderMock
            .Setup(d => d.UtcNow)
            .Returns(cachedAt);

        _fileStorageServiceMock
            .Setup(s => s.UploadFileAsync(
                "documents",
                job.DocumentName,
                document.Content,
                document.ContentType,
                It.Is<IDictionary<string, string>>(m =>
                    m["source_document_id"] == document.Id &&
                    m["cached_at"] == cachedAt.ToString("o") &&
                    m["source_last_modified"] == modifiedAt.ToString("o")),
                TestContext.Current.CancellationToken))
            .Returns(Task.CompletedTask);

        // Act & Assert
        await Should.NotThrowAsync(
            () => _handler.ExecuteAsync(job, TestContext.Current.CancellationToken));
    }

    [Fact(DisplayName = "Should return without throwing when document is not found")]
    public async Task ExecuteAsync_ShouldReturnGracefully_WhenDocumentNotFound()
    {
        // Arrange
        var job = new SyncDocumentToStorageJob
        {
            DocumentName = "non-existent",
            TriggeredBy = "scheduled"
        };

        _documentsServiceMock
            .Setup(s => s.GetDocumentAsHtmlAsync(job.DocumentName, TestContext.Current.CancellationToken))
            .ReturnsAsync((DocumentDto?)null);

        // Act & Assert
        await Should.NotThrowAsync(
            () => _handler.ExecuteAsync(job, TestContext.Current.CancellationToken));
    }

    [Fact(DisplayName = "Should propagate exception when upload fails")]
    public async Task ExecuteAsync_ShouldPropagateException_WhenUploadFails()
    {
        // Arrange
        var document = DocumentDtoFactory.Create();
        var cachedAt = new DateTimeOffset(2026, 2, 17, 7, 0, 0, TimeSpan.Zero);
        var job = new SyncDocumentToStorageJob
        {
            DocumentName = document.Name,
            TriggeredBy = "scheduled"
        };

        _documentsServiceMock
            .Setup(s => s.GetDocumentAsHtmlAsync(job.DocumentName, TestContext.Current.CancellationToken))
            .ReturnsAsync(document);

        _dateTimeProviderMock
            .Setup(d => d.UtcNow)
            .Returns(cachedAt);

        _fileStorageServiceMock
            .Setup(s => s.UploadFileAsync(
                "documents",
                job.DocumentName,
                document.Content,
                document.ContentType,
                It.IsAny<IDictionary<string, string>>(),
                TestContext.Current.CancellationToken))
            .ThrowsAsync(new InvalidOperationException("Storage unavailable"));

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _handler.ExecuteAsync(job, TestContext.Current.CancellationToken));

        exception.Message.ShouldBe("Storage unavailable");
    }

    [Fact(DisplayName = "Should propagate exception when document retrieval fails")]
    public async Task ExecuteAsync_ShouldPropagateException_WhenRetrievalFails()
    {
        // Arrange
        var job = new SyncDocumentToStorageJob
        {
            DocumentName = "bylaws",
            TriggeredBy = "scheduled"
        };

        _documentsServiceMock
            .Setup(s => s.GetDocumentAsHtmlAsync(job.DocumentName, TestContext.Current.CancellationToken))
            .ThrowsAsync(new HttpRequestException("Google Drive API error"));

        // Act & Assert
        var exception = await Should.ThrowAsync<HttpRequestException>(
            () => _handler.ExecuteAsync(job, TestContext.Current.CancellationToken));

        exception.Message.ShouldBe("Google Drive API error");
    }
}