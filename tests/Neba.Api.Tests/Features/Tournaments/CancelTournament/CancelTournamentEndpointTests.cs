using ErrorOr;

using FastEndpoints;

using Neba.Api.Features.Tournaments.CancelTournament;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Features.Tournaments.CancelTournament;

[UnitTest]
[Component("Tournaments")]
public sealed class CancelTournamentEndpointTests
{
    private const string ValidTournamentId = "01000000000000000000000001";

    [Fact(DisplayName = "HandleAsync should return 204 NoContent when the tournament is cancelled")]
    public async Task HandleAsync_ShouldReturn204_WhenTournamentIsCancelled()
    {
        // Arrange
        var request = new CancelTournamentRequest { Id = ValidTournamentId };
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CancelTournamentCommand, Success>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<CancelTournamentCommand>(), ct))
            .ReturnsAsync(Result.Success);

        var endpoint = Factory.Create<CancelTournamentEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
    }

    [Fact(DisplayName = "HandleAsync should map the request id to the command's TournamentId")]
    public async Task HandleAsync_ShouldMapRequestId_ToCommandTournamentId()
    {
        // Arrange
        var request = new CancelTournamentRequest { Id = ValidTournamentId };
        var ct = TestContext.Current.CancellationToken;
        CancelTournamentCommand? capturedCommand = null;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CancelTournamentCommand, Success>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<CancelTournamentCommand>(), ct))
            .Callback<CancelTournamentCommand, CancellationToken>((c, _) => capturedCommand = c)
            .ReturnsAsync(Result.Success);

        var endpoint = Factory.Create<CancelTournamentEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        capturedCommand.ShouldNotBeNull();
        capturedCommand.TournamentId.ShouldBe(new TournamentId(ValidTournamentId));
    }

    [Fact(DisplayName = "HandleAsync should return 404 when the command returns a not-found error")]
    public async Task HandleAsync_ShouldReturn404_WhenCommandReturnsNotFoundError()
    {
        // Arrange
        var request = new CancelTournamentRequest { Id = ValidTournamentId };
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CancelTournamentCommand, Success>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<CancelTournamentCommand>(), ct))
            .ReturnsAsync(TournamentErrors.TournamentNotFound(new TournamentId(ValidTournamentId)));

        var endpoint = Factory.Create<CancelTournamentEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }

    [Fact(DisplayName = "HandleAsync should return 409 when the command returns a conflict error")]
    public async Task HandleAsync_ShouldReturn409_WhenCommandReturnsConflictError()
    {
        // Arrange
        var request = new CancelTournamentRequest { Id = ValidTournamentId };
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CancelTournamentCommand, Success>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<CancelTournamentCommand>(), ct))
            .ReturnsAsync(TournamentErrors.TournamentAlreadyFinalized);

        var endpoint = Factory.Create<CancelTournamentEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(409);
    }

    [Fact(DisplayName = "Configure should register a permission-protected PATCH route under /tournaments")]
    public void Configure_ShouldRegisterPermissionProtectedPatchRoute_UnderTournamentsPath()
    {
        // Arrange
        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CancelTournamentCommand, Success>>(MockBehavior.Strict);
        var endpoint = Factory.Create<CancelTournamentEndpoint>(commandHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("PATCH");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("tournaments") && r.Contains("cancel"), "should be under the /tournaments/{id}/cancel path");
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
    }
}
