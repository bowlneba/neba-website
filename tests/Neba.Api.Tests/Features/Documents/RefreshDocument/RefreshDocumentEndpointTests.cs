using ErrorOr;

using FastEndpoints;

using Neba.Api.Contracts.Documents.RefreshDocument;
using Neba.Api.Features.Documents.RefreshDocument;
using Neba.TestFactory.Attributes;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Features.Documents.RefreshDocument;

[UnitTest]
[Component("Documents")]
public sealed class RefreshDocumentEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return 204 when the document cache is cleared")]
    public async Task HandleAsync_ShouldReturn204_WhenDocumentCacheIsCleared()
    {
        // Arrange
        const string documentName = "bylaws";
        var cancellationToken = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<RefreshDocumentCommand, Deleted>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(handler => handler.HandleAsync(
                It.Is<RefreshDocumentCommand>(c => c.DocumentName == documentName),
                cancellationToken))
            .ReturnsAsync(Result.Deleted);

        var endpoint = Factory.Create<RefreshDocumentEndpoint>(commandHandlerMock.Object);

        var request = new RefreshDocumentRequest
        {
            DocumentName = documentName
        };

        // Act
        await endpoint.HandleAsync(request, cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
    }

    [Fact(DisplayName = "HandleAsync should map request to command correctly")]
    public async Task HandleAsync_ShouldMapRequestToCommand_Correctly()
    {
        // Arrange
        const string documentName = "tournament-rules";
        var cancellationToken = TestContext.Current.CancellationToken;

        RefreshDocumentCommand? capturedCommand = null;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<RefreshDocumentCommand, Deleted>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(handler => handler.HandleAsync(
                It.IsAny<RefreshDocumentCommand>(),
                cancellationToken))
            .ReturnsAsync(Result.Deleted)
            .Callback<RefreshDocumentCommand, CancellationToken>((command, _) => capturedCommand = command);

        var endpoint = Factory.Create<RefreshDocumentEndpoint>(commandHandlerMock.Object);

        var request = new RefreshDocumentRequest
        {
            DocumentName = documentName
        };

        // Act
        await endpoint.HandleAsync(request, cancellationToken);

        // Assert
        capturedCommand.ShouldNotBeNull();
        capturedCommand.DocumentName.ShouldBe(documentName);
    }

    [Fact(DisplayName = "Configure should register DELETE route at documents/{DocumentName}/cache")]
    public void Configure_ShouldRegisterDeleteRoute_AtExpectedPath()
    {
        // Arrange
        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<RefreshDocumentCommand, Deleted>>(MockBehavior.Strict);
        var endpoint = Factory.Create<RefreshDocumentEndpoint>(commandHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("DELETE");
        endpoint.Definition.Routes.ShouldContain("/documents/{DocumentName}/cache");
    }
}
