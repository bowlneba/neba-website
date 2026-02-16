using Neba.Application.Documents;
using Neba.Application.Documents.GetDocument;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Documents;

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
        var expectedDocument = DocumentDtoFactory.Create();

        _documentsServiceMock
            .Setup(service => service.GetDocumentAsHtmlAsync(expectedDocument.Name, TestContext.Current.CancellationToken))
            .ReturnsAsync(expectedDocument);

        var query = new GetDocumentQuery
        {
            DocumentName = expectedDocument.Name
        };

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(expectedDocument.Content);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnDocumentNotFoundError_WhenDocumentDoesNotExist()
    {
        // Arrange
        const string documentName = "NonExistentDocument";

        _documentsServiceMock
            .Setup(service => service.GetDocumentAsHtmlAsync(documentName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentDto?)null);

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