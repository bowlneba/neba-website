using ErrorOr;

using FastEndpoints;

using Neba.Api.Contracts.Documents.GetDocument;
using Neba.Api.Features.Documents.GetDocument;
using Neba.Application.Documents.GetDocument;
using Neba.Api.Messaging;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Documents;

namespace Neba.Api.Tests.Features.Documents.GetDocument;

[UnitTest]
[Component("Documents")]
public sealed class GetDocumentEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return OK with HTML when query succeeds")]
    public async Task HandleAsync_ShouldReturnOkWithHtml_WhenQuerySucceeds()
    {
        // Arrange
        const string documentName = "TestDocument";
        var dto = GetDocumentDtoFactory.Create();

        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<GetDocumentQuery, ErrorOr<GetDocumentDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(
                It.Is<GetDocumentQuery>(q => q.DocumentName == documentName),
                cancellationToken))
            .ReturnsAsync(dto.ToErrorOr());

        var endpoint = Factory.Create<GetDocumentEndpoint>(
            queryHandlerMock.Object);

        var request = new GetDocumentRequest
        {
            DocumentName = documentName
        };

        // Act
        await endpoint.HandleAsync(request, cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.HttpContext.Response.ContentType.ShouldNotBeNull();
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Html.ShouldBe(dto.Html);
        endpoint.Response.LastUpdated.ShouldBe(dto.LastUpdated);
    }

    [Fact(DisplayName = "HandleAsync should return 404 when document not found")]
    public async Task HandleAsync_ShouldReturn404_WhenDocumentNotFound()
    {
        // Arrange
        const string documentName = "NonExistentDocument";

        var cancellationToken = TestContext.Current.CancellationToken;

        var error = Error.NotFound(
            "Document.NotFound",
            $"Document with name '{documentName}' was not found.");

        var queryHandlerMock = new Mock<IQueryHandler<GetDocumentQuery, ErrorOr<GetDocumentDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(
                It.Is<GetDocumentQuery>(q => q.DocumentName == documentName),
                cancellationToken))
            .ReturnsAsync(error);

        var endpoint = Factory.Create<GetDocumentEndpoint>(
            queryHandlerMock.Object);

        var request = new GetDocumentRequest
        {
            DocumentName = documentName
        };

        // Act
        await endpoint.HandleAsync(request, cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
        endpoint.HttpContext.Response.ContentType.ShouldNotBe("application/json; charset=utf-8");
    }

    [Fact(DisplayName = "Configure should register anonymous GET route at /documents/{DocumentName}")]
    public void Configure_ShouldRegisterAnonymousGetRoute_AtExpectedPath()
    {
        // Arrange
        var queryHandlerMock = new Mock<IQueryHandler<GetDocumentQuery, ErrorOr<GetDocumentDto>>>(MockBehavior.Strict);
        var endpoint = Factory.Create<GetDocumentEndpoint>(queryHandlerMock.Object);

        // Assert — route and auth
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain("/documents/{DocumentName}");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();

    }

    [Fact(DisplayName = "HandleAsync should map request to query correctly")]
    public async Task HandleAsync_ShouldMapRequestToQuery_Correctly()
    {
        // Arrange
        const string documentName = "privacy-policy";
        var dto = GetDocumentDtoFactory.Create();

        var cancellationToken = TestContext.Current.CancellationToken;

        GetDocumentQuery? capturedQuery = null;

        var queryHandlerMock = new Mock<IQueryHandler<GetDocumentQuery, ErrorOr<GetDocumentDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(
                It.IsAny<GetDocumentQuery>(),
                cancellationToken))
            .ReturnsAsync(dto.ToErrorOr())
            .Callback<GetDocumentQuery, CancellationToken>((query, _) => capturedQuery = query);

        var endpoint = Factory.Create<GetDocumentEndpoint>(
            queryHandlerMock.Object);

        var request = new GetDocumentRequest
        {
            DocumentName = documentName
        };

        // Act
        await endpoint.HandleAsync(request, cancellationToken);

        // Assert
        capturedQuery.ShouldNotBeNull();
        capturedQuery.DocumentName.ShouldBe(documentName);
    }
}