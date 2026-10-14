using ErrorOr;

using FastEndpoints;

using Neba.Api.Contracts.Tournaments.CreateTournament;
using Neba.Api.Contracts.Tournaments.EditTournamentLogo;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.EditTournamentLogo;
using Neba.TestFactory.Attributes;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Features.Tournaments.EditTournamentLogo;

[UnitTest]
[Component("Tournaments")]
public sealed class EditTournamentLogoEndpointTests
{
    [Fact(DisplayName = "HandleAsync should map the request to the command and return 204 when the edit succeeds")]
    public async Task HandleAsync_ShouldMapRequestAndReturn204_WhenEditSucceeds()
    {
        // Arrange
        var tournamentId = TournamentId.New();
        var logo = new TournamentLogoInput
        {
            Container = "bowlneba-public",
            Path = "tournaments/logo/new.png",
            ContentType = "image/png",
            SizeInBytes = 4096
        };
        var request = new EditTournamentLogoRequest { Id = tournamentId.Value.ToString(), Logo = logo };
        var ct = TestContext.Current.CancellationToken;

        EditTournamentLogoCommand? capturedCommand = null;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<EditTournamentLogoCommand, Updated>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<EditTournamentLogoCommand>(), ct))
            .Callback<EditTournamentLogoCommand, CancellationToken>((c, _) => capturedCommand = c)
            .ReturnsAsync(Result.Updated);

        var endpoint = Factory.Create<EditTournamentLogoEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        capturedCommand.ShouldNotBeNull();
        capturedCommand.TournamentId.ShouldBe(tournamentId);
        capturedCommand.Logo.ShouldNotBeNull();
        capturedCommand.Logo.Container.ShouldBe(logo.Container);
        capturedCommand.Logo.Path.ShouldBe(logo.Path);
        capturedCommand.Logo.ContentType.ShouldBe(logo.ContentType);
        capturedCommand.Logo.SizeInBytes.ShouldBe(logo.SizeInBytes);
    }

    [Fact(DisplayName = "HandleAsync should send a null logo on the command when the request logo is null")]
    public async Task HandleAsync_ShouldSendNullLogo_WhenRequestLogoIsNull()
    {
        // Arrange
        var request = new EditTournamentLogoRequest { Id = TournamentId.New().Value.ToString(), Logo = null };
        var ct = TestContext.Current.CancellationToken;

        EditTournamentLogoCommand? capturedCommand = null;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<EditTournamentLogoCommand, Updated>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<EditTournamentLogoCommand>(), ct))
            .Callback<EditTournamentLogoCommand, CancellationToken>((c, _) => capturedCommand = c)
            .ReturnsAsync(Result.Updated);

        var endpoint = Factory.Create<EditTournamentLogoEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        capturedCommand.ShouldNotBeNull();
        capturedCommand.Logo.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should return 404 when the command returns a not-found error")]
    public async Task HandleAsync_ShouldReturn404_WhenCommandReturnsNotFoundError()
    {
        // Arrange
        var request = new EditTournamentLogoRequest { Id = TournamentId.New().Value.ToString(), Logo = null };
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<EditTournamentLogoCommand, Updated>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<EditTournamentLogoCommand>(), ct))
            .ReturnsAsync(TournamentErrors.TournamentNotFound(TournamentId.New()));

        var endpoint = Factory.Create<EditTournamentLogoEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }
}
