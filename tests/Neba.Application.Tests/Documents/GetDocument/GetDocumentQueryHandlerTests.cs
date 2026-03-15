using ErrorOr;

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

    [Fact(DisplayName = "Should return cached content and source_last_modified as LastUpdated when file exists in storage")]
    public async Task HandleAsync_ShouldReturnCachedContentAndLastUpdated_WhenFileExistsInStorage()
    {
        // Arrange
        var lastModified = new DateTimeOffset(2026, 1, 15, 5, 0, 0, TimeSpan.Zero);
        var storedFile = FileContentFactory.Create(
            content: DocumentDtoFactory.ValidContent,
            metadata: new Dictionary<string, string>
            {
                { "source_document_id", DocumentDtoFactory.ValidId },
                { "cached_at", new DateTimeOffset(2026, 2, 1, 5, 0, 0, TimeSpan.Zero).ToString("o") },
                { "source_last_modified", lastModified.ToString("o") }
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
        result.Value.LastUpdated.ShouldBe(lastModified);
    }

    [Fact(DisplayName = "Should return cached content with null LastUpdated when source_last_modified metadata is absent")]
    public async Task HandleAsync_ShouldReturnNullLastUpdated_WhenSourceLastModifiedMetadataAbsent()
    {
        // Arrange
        var storedFile = FileContentFactory.Create(
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
        result.Value.LastUpdated.ShouldBeNull();
    }

    [Fact(DisplayName = "Should use cached file and not call documents service when file exists in storage")]
    public async Task HandleAsync_ShouldUseCachedFile_AndNotCallDocumentsService_WhenFileExistsInStorage()
    {
        // Arrange
        const string cachedContent = "<h1>Cached Content</h1>";
        var storedFile = FileContentFactory.Create(content: cachedContent);
        var query = new GetDocumentQuery { DocumentName = DocumentDtoFactory.ValidName };

        _storageServiceMock
            .Setup(s => s.ExistsAsync("documents", query.DocumentName, TestContext.Current.CancellationToken))
            .ReturnsAsync(true);

        _storageServiceMock
            .Setup(s => s.GetFileAsync("documents", query.DocumentName, TestContext.Current.CancellationToken))
            .ReturnsAsync(storedFile);

        // Set up non-cached path so no exception is thrown if mutation negates ExistsAsync:
        // if the wrong path is taken, different content is returned and the Verify below fails.
        var sourceDocument = DocumentDtoFactory.Create(content: "<h1>Source Content</h1>");
        _documentsServiceMock
            .Setup(s => s.GetDocumentAsHtmlAsync(query.DocumentName, TestContext.Current.CancellationToken))
            .ReturnsAsync(sourceDocument);
        _dateTimeProviderMock
            .Setup(d => d.UtcNow)
            .Returns(new DateTimeOffset(2026, 2, 1, 5, 0, 0, TimeSpan.Zero));
        _storageServiceMock
            .Setup(s => s.UploadFileAsync(
                "documents", query.DocumentName,
                sourceDocument.Content, sourceDocument.ContentType,
                It.IsAny<IDictionary<string, string>>(),
                TestContext.Current.CancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Html.ShouldBe(cachedContent);
        _documentsServiceMock.Verify(
            s => s.GetDocumentAsHtmlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    // Mutation 2 (block removal: if file is null { return NotFound }):
    // wrapped in try/catch so NullReferenceException becomes a Shouldly assertion failure
    // (Stryker MTP runner doesn't surface handler exceptions as test failures directly).
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
            .ReturnsAsync((FileContent?)null);

        // Act — wrap so any handler exception becomes an assertion failure
        Exception? handlerException = null;
        ErrorOr<GetDocumentDto> result = default!;
        try
        {
            result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);
        }
        catch (Exception ex)
        {
            handlerException = ex;
        }

        // Assert
        handlerException.ShouldBeNull($"Handler threw unexpectedly: {handlerException?.GetType().Name}");
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Document.NotFound");
    }

    // Mutations 4 (UploadFileAsync removed), 5 (empty metadata), 6 (null-coalescing → always empty):
    // captured via Callback so assertions are Shouldly-based, not reliant on mock exceptions.
    [Fact(DisplayName = "Should fetch from documents service, upload to storage with source_last_modified, and return LastUpdated when not cached")]
    public async Task HandleAsync_ShouldFetchUploadAndReturnLastUpdated_WhenNotCached()
    {
        // Arrange
        var modifiedAt = new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero);
        var expectedDocument = DocumentDtoFactory.Create(modifiedAt: modifiedAt);
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

        IDictionary<string, string>? capturedMetadata = null;
        _storageServiceMock
            .Setup(s => s.UploadFileAsync(
                "documents",
                query.DocumentName,
                expectedDocument.Content,
                expectedDocument.ContentType,
                It.IsAny<IDictionary<string, string>>(),
                TestContext.Current.CancellationToken))
            .Callback<string, string, string, string, IDictionary<string, string>, CancellationToken>(
                (_, _, _, _, metadata, _) => capturedMetadata = metadata)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Html.ShouldBe(expectedDocument.Content);
        result.Value.LastUpdated.ShouldBe(modifiedAt);
        capturedMetadata.ShouldNotBeNull("UploadFileAsync should have been called with metadata");
        capturedMetadata!.ShouldContainKeyAndValue("source_document_id", expectedDocument.Id);
        capturedMetadata.ShouldContainKeyAndValue("cached_at", cachedAt.ToString("o"));
        capturedMetadata.ShouldContainKeyAndValue("source_last_modified", modifiedAt.ToString("o"));
    }

    [Fact(DisplayName = "Should upload document with empty source_last_modified when document has no ModifiedAt")]
    public async Task HandleAsync_ShouldUploadWithEmptySourceLastModified_WhenDocumentModifiedAtIsNull()
    {
        // Arrange
        var expectedDocument = DocumentDtoFactory.Create(modifiedAt: null);
        var cachedAt = new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);
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

        IDictionary<string, string>? capturedMetadata = null;
        _storageServiceMock
            .Setup(s => s.UploadFileAsync(
                "documents",
                query.DocumentName,
                expectedDocument.Content,
                expectedDocument.ContentType,
                It.IsAny<IDictionary<string, string>>(),
                TestContext.Current.CancellationToken))
            .Callback<string, string, string, string, IDictionary<string, string>, CancellationToken>(
                (_, _, _, _, metadata, _) => capturedMetadata = metadata)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.LastUpdated.ShouldBeNull();
        capturedMetadata.ShouldNotBeNull("UploadFileAsync should have been called with metadata");
        capturedMetadata!.ShouldContainKeyAndValue("source_last_modified", string.Empty);
    }

    // Mutation 3 (block removal: if document is null { return NotFound }):
    // wrapped in try/catch so NullReferenceException becomes a Shouldly assertion failure.
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

        // Act — wrap so any handler exception becomes an assertion failure
        Exception? handlerException = null;
        ErrorOr<GetDocumentDto> result = default!;
        try
        {
            result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);
        }
        catch (Exception ex)
        {
            handlerException = ex;
        }

        // Assert
        handlerException.ShouldBeNull($"Handler threw unexpectedly: {handlerException?.GetType().Name}");
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Document.NotFound");
        result.FirstError.Description.ShouldBe($"Document with name '{documentName}' was not found.");
    }
}
