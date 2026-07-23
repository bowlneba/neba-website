using ErrorOr;

using FastEndpoints;

using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.RemoveTournamentSponsor;
using Neba.TestFactory.Attributes;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Features.Tournaments.RemoveTournamentSponsor;

[UnitTest]
[Component("Tournaments")]
public sealed class RemoveTournamentSponsorEndpointTests
{
    private const string ValidTournamentId = "01000000000000000000000001";
    private const string ValidSponsorId = "01000000000000000000000002";

    private static RemoveTournamentSponsorRequest ValidRequest()
        => new() { TournamentId = ValidTournamentId, SponsorId = ValidSponsorId };

    [Fact(DisplayName = "HandleAsync should map request fields to command and take the success branch when removing succeeds")]
    public async Task HandleAsync_ShouldMapRequestToCommandAndTakeSuccessBranch_WhenRemovingSucceeds()
    {
        // Arrange
        var request = ValidRequest();
        var ct = TestContext.Current.CancellationToken;

        RemoveTournamentSponsorCommand? capturedCommand = null;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<RemoveTournamentSponsorCommand, Deleted>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<RemoveTournamentSponsorCommand>(), ct))
            .Callback<RemoveTournamentSponsorCommand, CancellationToken>((c, _) => capturedCommand = c)
            .ReturnsAsync(Result.Deleted);

        var endpoint = Factory.Create<RemoveTournamentSponsorEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        capturedCommand.ShouldNotBeNull();
        capturedCommand.TournamentId.ShouldBe(new TournamentId(ValidTournamentId));
        capturedCommand.SponsorId.ShouldBe(new SponsorId(ValidSponsorId));
    }

    [Fact(DisplayName = "HandleAsync should return 404 when the command returns a not-found error")]
    public async Task HandleAsync_ShouldReturn404_WhenCommandReturnsNotFoundError()
    {
        // Arrange
        var request = ValidRequest();
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<RemoveTournamentSponsorCommand, Deleted>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<RemoveTournamentSponsorCommand>(), ct))
            .ReturnsAsync(TournamentErrors.TournamentNotFound(new TournamentId(ValidTournamentId)));

        var endpoint = Factory.Create<RemoveTournamentSponsorEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }

    [Fact(DisplayName = "HandleAsync should return 409 when the command returns a conflict error")]
    public async Task HandleAsync_ShouldReturn409_WhenCommandReturnsConflictError()
    {
        // Arrange
        var request = ValidRequest();
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<RemoveTournamentSponsorCommand, Deleted>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<RemoveTournamentSponsorCommand>(), ct))
            .ReturnsAsync(TournamentErrors.SponsorNotAttached(new SponsorId(ValidSponsorId)));

        var endpoint = Factory.Create<RemoveTournamentSponsorEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(409);
    }

    [Fact(DisplayName = "Configure should register a permission-protected DELETE route under /tournaments")]
    public void Configure_ShouldRegisterPermissionProtectedDeleteRoute_UnderTournamentsPath()
    {
        // Arrange
        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<RemoveTournamentSponsorCommand, Deleted>>(MockBehavior.Strict);
        var endpoint = Factory.Create<RemoveTournamentSponsorEndpoint>(commandHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("DELETE");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("tournaments"), "should be under the /tournaments path");
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
    }
}
