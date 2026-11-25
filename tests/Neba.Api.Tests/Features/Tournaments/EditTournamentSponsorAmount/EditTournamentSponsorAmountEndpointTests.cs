using ErrorOr;

using FastEndpoints;

using Neba.Api.Contracts.Tournaments.EditTournamentSponsorAmount;
using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.EditTournamentSponsorAmount;
using Neba.TestFactory.Attributes;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Features.Tournaments.EditTournamentSponsorAmount;

[UnitTest]
[Component("Tournaments")]
public sealed class EditTournamentSponsorAmountEndpointTests
{
    private const string ValidTournamentId = "01000000000000000000000001";
    private const string ValidSponsorId = "01000000000000000000000002";

    private static EditTournamentSponsorAmountRequest ValidRequest(decimal sponsorshipAmount = 500m)
        => new()
        {
            Id = ValidTournamentId,
            SponsorId = ValidSponsorId,
            Sponsor = new EditTournamentSponsorAmountInput
            {
                SponsorshipAmount = sponsorshipAmount
            }
        };

    [Fact(DisplayName = "HandleAsync should map request fields to command and take the success branch when editing succeeds")]
    public async Task HandleAsync_ShouldMapRequestToCommandAndTakeSuccessBranch_WhenEditingSucceeds()
    {
        // Arrange
        var request = ValidRequest(sponsorshipAmount: 1234.56m);
        var ct = TestContext.Current.CancellationToken;

        EditTournamentSponsorAmountCommand? capturedCommand = null;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<EditTournamentSponsorAmountCommand, Success>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<EditTournamentSponsorAmountCommand>(), ct))
            .Callback<EditTournamentSponsorAmountCommand, CancellationToken>((c, _) => capturedCommand = c)
            .ReturnsAsync(Result.Success);

        var endpoint = Factory.Create<EditTournamentSponsorAmountEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        capturedCommand.ShouldNotBeNull();
        capturedCommand.TournamentId.ShouldBe(new TournamentId(ValidTournamentId));
        capturedCommand.SponsorId.ShouldBe(new SponsorId(ValidSponsorId));
        capturedCommand.SponsorshipAmount.ShouldBe(1234.56m);
    }

    [Fact(DisplayName = "HandleAsync should return 404 when the command returns a not-found error")]
    public async Task HandleAsync_ShouldReturn404_WhenCommandReturnsNotFoundError()
    {
        // Arrange
        var request = ValidRequest();
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<EditTournamentSponsorAmountCommand, Success>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<EditTournamentSponsorAmountCommand>(), ct))
            .ReturnsAsync(TournamentErrors.TournamentNotFound(new TournamentId(ValidTournamentId)));

        var endpoint = Factory.Create<EditTournamentSponsorAmountEndpoint>(commandHandlerMock.Object);

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

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<EditTournamentSponsorAmountCommand, Success>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<EditTournamentSponsorAmountCommand>(), ct))
            .ReturnsAsync(TournamentErrors.SponsorNotAttached(new SponsorId(ValidSponsorId)));

        var endpoint = Factory.Create<EditTournamentSponsorAmountEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(409);
    }

    [Fact(DisplayName = "HandleAsync should return 422 when the command returns a validation error")]
    public async Task HandleAsync_ShouldReturn422_WhenCommandReturnsValidationError()
    {
        // Arrange
        var request = ValidRequest();
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<EditTournamentSponsorAmountCommand, Success>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<EditTournamentSponsorAmountCommand>(), ct))
            .ReturnsAsync(TournamentSponsorErrors.NegativeSponsorshipAmount);

        var endpoint = Factory.Create<EditTournamentSponsorAmountEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(422);
    }

    [Fact(DisplayName = "Configure should register a permission-protected PUT route under /tournaments")]
    public void Configure_ShouldRegisterPermissionProtectedPutRoute_UnderTournamentsPath()
    {
        // Arrange
        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<EditTournamentSponsorAmountCommand, Success>>(MockBehavior.Strict);
        var endpoint = Factory.Create<EditTournamentSponsorAmountEndpoint>(commandHandlerMock.Object);

        // Act
        var definition = endpoint.Definition;

        // Assert
        definition.Verbs.ShouldContain("PUT");
        definition.Routes.ShouldContain(r => r.Contains("tournaments"), "should be under the /tournaments path");
        definition.AnonymousVerbs.ShouldBeNull();
    }
}