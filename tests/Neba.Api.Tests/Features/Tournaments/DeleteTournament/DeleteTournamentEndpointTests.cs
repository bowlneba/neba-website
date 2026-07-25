using ErrorOr;

using FastEndpoints;

using Neba.Api.Features.Tournaments.DeleteTournament;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Features.Tournaments.DeleteTournament;

[UnitTest]
[Component("Tournaments")]
public sealed class DeleteTournamentEndpointTests
{
    private const string ValidTournamentId = "01000000000000000000000001";

    [Fact(DisplayName = "HandleAsync should return 204 NoContent when tournament is deleted")]
    public async Task HandleAsync_ShouldReturn204_WhenTournamentIsDeleted()
    {
        // Arrange
        var request = new DeleteTournamentRequest { Id = ValidTournamentId };
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<DeleteTournamentCommand, Deleted>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<DeleteTournamentCommand>(), ct))
            .ReturnsAsync(Result.Deleted);

        var endpoint = Factory.Create<DeleteTournamentEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
    }

    [Fact(DisplayName = "HandleAsync should map the request id to the command's TournamentId")]
    public async Task HandleAsync_ShouldMapRequestId_ToCommandTournamentId()
    {
        // Arrange
        var request = new DeleteTournamentRequest { Id = ValidTournamentId };
        var ct = TestContext.Current.CancellationToken;
        DeleteTournamentCommand? capturedCommand = null;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<DeleteTournamentCommand, Deleted>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<DeleteTournamentCommand>(), ct))
            .Callback<DeleteTournamentCommand, CancellationToken>((c, _) => capturedCommand = c)
            .ReturnsAsync(Result.Deleted);

        var endpoint = Factory.Create<DeleteTournamentEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        capturedCommand.ShouldNotBeNull();
        capturedCommand.TournamentId.ShouldBe(new TournamentId(ValidTournamentId));
    }

    [Fact(DisplayName = "HandleAsync should return 409 when the command returns a conflict error")]
    public async Task HandleAsync_ShouldReturn409_WhenCommandReturnsConflictError()
    {
        // Arrange
        var request = new DeleteTournamentRequest { Id = ValidTournamentId };
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<DeleteTournamentCommand, Deleted>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<DeleteTournamentCommand>(), ct))
            .ReturnsAsync(TournamentErrors.HasHistoricalRecords(new TournamentId(ValidTournamentId)));

        var endpoint = Factory.Create<DeleteTournamentEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(409);
    }

    [Fact(DisplayName = "Configure should register a permission-protected DELETE route under /tournaments")]
    public void Configure_ShouldRegisterPermissionProtectedDeleteRoute_UnderTournamentsPath()
    {
        // Arrange
        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<DeleteTournamentCommand, Deleted>>(MockBehavior.Strict);
        var endpoint = Factory.Create<DeleteTournamentEndpoint>(commandHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("DELETE");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("tournaments"), "should be under the /tournaments path");
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
    }
}
