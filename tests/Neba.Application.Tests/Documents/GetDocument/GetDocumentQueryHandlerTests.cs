using Neba.Application.Documents;
using Neba.Application.Documents.GetDocument;
using Neba.TestFactory.Attributes;

namespace Neba.Application.Tests.Documents.GetDocument;

[UnitTest]
[Component("GetDocumentQueryHandler")]
public sealed class GetDocumentQueryHandlerTests
{
    private readonly Mock<IDocumentsService> _documentsServiceMock;

    private readonly GetDocumentQueryHandler _handler;

    public GetDocumentQueryHandlerTests()
    {
        _documentsServiceMock = new Mock<IDocumentsService>(MockBehavior.Strict);
        _handler = new GetDocumentQueryHandler(_documentsServiceMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnDocumentContent()
    {
        // Arrange
        const string documentName = "TestDocument";
        const string expectedContent = "<html><body>Test Document Content</body></html>";

        _documentsServiceMock
            .Setup(service => service.GetDocumentAsHtmlAsync(documentName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedContent);

        var query = new GetDocumentQuery
        {
            DocumentName = documentName
        };

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(expectedContent);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnDocumentNotFoundError_WhenDocumentDoesNotExist()
    {
        // Arrange
        const string documentName = "NonExistentDocument";

        _documentsServiceMock
            .Setup(service => service.GetDocumentAsHtmlAsync(documentName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var query = new GetDocumentQuery
        {
            DocumentName = documentName
        };

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Document.NotFound");
        result.FirstError.Description.ShouldBe($"Document with name '{documentName}' was not found.");
    }
}