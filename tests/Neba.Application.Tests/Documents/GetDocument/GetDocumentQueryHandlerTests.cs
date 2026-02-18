using Neba.Application.Clock;
using Neba.Application.Documents;
using Neba.Application.Documents.GetDocument;
using Neba.Application.Storage;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Documents;
using Neba.TestFactory.Storage;

namespace Neba.Application.Tests.Documents.GetDocument;

[UnitTest]
[Component("Documents")]
public sealed class GetDocumentQueryHandlerTests
{
    private readonly Mock<IDocumentsService> _documentsServiceMock;
    private readonly Mock<IFileStorageService> _storageServiceMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;

    private readonly GetDocumentQueryHandler _handler;

    public GetDocumentQueryHandlerTests()
    {
        _documentsServiceMock = new Mock<IDocumentsService>(MockBehavior.Strict);
        _storageServiceMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        _dateTimeProviderMock = new Mock<IDateTimeProvider>(MockBehavior.Strict);

        _handler = new GetDocumentQueryHandler(
            _documentsServiceMock.Object,
            _storageServiceMock.Object,
            _dateTimeProviderMock.Object);
    }

    [Fact(DisplayName = "Should return cached content and cached_at when file exists in storage")]
    public async Task HandleAsync_ShouldReturnCachedContentAndCachedAt_WhenFileExistsInStorage()
    {
        // Arrange
        var cachedAt = new DateTimeOffset(2026, 1, 15, 5, 0, 0, TimeSpan.Zero);
        var storedFile = StoredFileFactory.Create(
            content: DocumentDtoFactory.ValidContent,
            metadata: new Dictionary<string, string>
            {
                { "source_document_id", DocumentDtoFactory.ValidId },
                { "cached_at", cachedAt.ToString("o") }
            });
        var query = new GetDocumentQuery { DocumentName = DocumentDtoFactory.ValidName };

        _storageServiceMock
            .Setup(s => s.ExistsAsync("documents", query.DocumentName, TestContext.Current.CancellationToken))
            .ReturnsAsync(true);

        _storageServiceMock
            .Setup(s => s.GetFileAsync("documents", query.DocumentName, TestContext.Current.CancellationToken))
            .ReturnsAsync(storedFile);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Html.ShouldBe(storedFile.Content);
        result.Value.CachedAt.ShouldBe(cachedAt);
    }

    [Fact(DisplayName = "Should return cached content with null CachedAt when cached_at metadata is absent")]
    public async Task HandleAsync_ShouldReturnNullCachedAt_WhenCachedAtMetadataAbsent()
    {
        // Arrange
        var storedFile = StoredFileFactory.Create(
            content: DocumentDtoFactory.ValidContent,
            metadata: new Dictionary<string, string> { { "source_document_id", DocumentDtoFactory.ValidId } });
        var query = new GetDocumentQuery { DocumentName = DocumentDtoFactory.ValidName };

        _storageServiceMock
            .Setup(s => s.ExistsAsync("documents", query.DocumentName, TestContext.Current.CancellationToken))
            .ReturnsAsync(true);

        _storageServiceMock
            .Setup(s => s.GetFileAsync("documents", query.DocumentName, TestContext.Current.CancellationToken))
            .ReturnsAsync(storedFile);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Html.ShouldBe(storedFile.Content);
        result.Value.CachedAt.ShouldBeNull();
    }

    [Fact(DisplayName = "Should return not found when file exists in storage but GetFile returns null")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenFileExistsButGetFileReturnsNull()
    {
        // Arrange
        var query = new GetDocumentQuery { DocumentName = DocumentDtoFactory.ValidName };

        _storageServiceMock
            .Setup(s => s.ExistsAsync("documents", query.DocumentName, TestContext.Current.CancellationToken))
            .ReturnsAsync(true);

        _storageServiceMock
            .Setup(s => s.GetFileAsync("documents", query.DocumentName, TestContext.Current.CancellationToken))
            .ReturnsAsync((StoredFile?)null);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Document.NotFound");
    }

    [Fact(DisplayName = "Should fetch from documents service, upload to storage, and return CachedAt when not cached")]
    public async Task HandleAsync_ShouldFetchUploadAndReturnCachedAt_WhenNotCached()
    {
        // Arrange
        var expectedDocument = DocumentDtoFactory.Create();
        var cachedAt = new DateTimeOffset(2026, 2, 16, 12, 0, 0, TimeSpan.Zero);
        var query = new GetDocumentQuery { DocumentName = expectedDocument.Name };

        _storageServiceMock
            .Setup(s => s.ExistsAsync("documents", query.DocumentName, TestContext.Current.CancellationToken))
            .ReturnsAsync(false);

        _documentsServiceMock
            .Setup(s => s.GetDocumentAsHtmlAsync(query.DocumentName, TestContext.Current.CancellationToken))
            .ReturnsAsync(expectedDocument);

        _dateTimeProviderMock
            .Setup(d => d.UtcNow)
            .Returns(cachedAt);

        _storageServiceMock
            .Setup(s => s.UploadFileAsync(
                "documents",
                query.DocumentName,
                expectedDocument.Content,
                expectedDocument.ContentType,
                It.Is<IDictionary<string, string>>(m =>
                    m["source_document_id"] == expectedDocument.Id &&
                    m["cached_at"] == cachedAt.ToString("o")),
                TestContext.Current.CancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Html.ShouldBe(expectedDocument.Content);
        result.Value.CachedAt.ShouldBe(cachedAt);
    }

    [Fact(DisplayName = "Should return not found when documents service returns null")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenDocumentsServiceReturnsNull()
    {
        // Arrange
        const string documentName = "non-existent";
        var query = new GetDocumentQuery { DocumentName = documentName };

        _storageServiceMock
            .Setup(s => s.ExistsAsync("documents", documentName, TestContext.Current.CancellationToken))
            .ReturnsAsync(false);

        _documentsServiceMock
            .Setup(s => s.GetDocumentAsHtmlAsync(documentName, TestContext.Current.CancellationToken))
            .ReturnsAsync((DocumentDto?)null);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Document.NotFound");
        result.FirstError.Description.ShouldBe($"Document with name '{documentName}' was not found.");
    }
}
