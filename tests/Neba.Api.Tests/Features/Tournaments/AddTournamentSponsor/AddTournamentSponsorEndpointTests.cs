using ErrorOr;

using FastEndpoints;

using Neba.Api.Contracts.Tournaments.AddTournamentSponsor;
using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Tournaments.AddTournamentSponsor;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Features.Tournaments.AddTournamentSponsor;

[UnitTest]
[Component("Tournaments")]
public sealed class AddTournamentSponsorEndpointTests
{
    private const string ValidTournamentId = "01000000000000000000000001";
    private const string ValidSponsorId = "01000000000000000000000002";

    private static AddTournamentSponsorRequest ValidRequest(
        bool titleSponsor = false,
        decimal sponsorshipAmount = 500m)
        => new()
        {
            Id = ValidTournamentId,
            Sponsor = new AddTournamentSponsorInput
            {
                SponsorId = ValidSponsorId,
                TitleSponsor = titleSponsor,
                SponsorshipAmount = sponsorshipAmount
            }
        };

    [Fact(DisplayName = "HandleAsync should map request fields to command and take the success branch when adding succeeds")]
    public async Task HandleAsync_ShouldMapRequestToCommandAndTakeSuccessBranch_WhenAddingSucceeds()
    {
        // Arrange
        var request = ValidRequest(titleSponsor: true, sponsorshipAmount: 1234.56m);
        var ct = TestContext.Current.CancellationToken;

        AddTournamentSponsorCommand? capturedCommand = null;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<AddTournamentSponsorCommand, Success>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<AddTournamentSponsorCommand>(), ct))
            .Callback<AddTournamentSponsorCommand, CancellationToken>((c, _) => capturedCommand = c)
            .ReturnsAsync(Result.Success);

        var endpoint = Factory.Create<AddTournamentSponsorEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        capturedCommand.ShouldNotBeNull();
        capturedCommand.TournamentId.ShouldBe(new TournamentId(ValidTournamentId));
        capturedCommand.SponsorId.ShouldBe(new SponsorId(ValidSponsorId));
        capturedCommand.TitleSponsor.ShouldBeTrue();
        capturedCommand.SponsorshipAmount.ShouldBe(1234.56m);
    }

    [Fact(DisplayName = "HandleAsync should return 404 when the command returns a not-found error")]
    public async Task HandleAsync_ShouldReturn404_WhenCommandReturnsNotFoundError()
    {
        // Arrange
        var request = ValidRequest();
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<AddTournamentSponsorCommand, Success>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<AddTournamentSponsorCommand>(), ct))
            .ReturnsAsync(TournamentErrors.TournamentNotFound(new TournamentId(ValidTournamentId)));

        var endpoint = Factory.Create<AddTournamentSponsorEndpoint>(commandHandlerMock.Object);

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

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<AddTournamentSponsorCommand, Success>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<AddTournamentSponsorCommand>(), ct))
            .ReturnsAsync(TournamentErrors.SponsorAlreadyAdded(new SponsorId(ValidSponsorId)));

        var endpoint = Factory.Create<AddTournamentSponsorEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(409);
    }

    [Fact(DisplayName = "HandleAsync should return 422 when the command returns validation errors")]
    public async Task HandleAsync_ShouldReturn422_WhenCommandReturnsValidationErrors()
    {
        // Arrange
        var request = ValidRequest();
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<AddTournamentSponsorCommand, Success>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<AddTournamentSponsorCommand>(), ct))
            .ReturnsAsync(TournamentErrors.SponsorNotFound(new SponsorId(ValidSponsorId)));

        var endpoint = Factory.Create<AddTournamentSponsorEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(422);
    }

    [Fact(DisplayName = "Configure should register a permission-protected POST route under /tournaments")]
    public void Configure_ShouldRegisterPermissionProtectedPostRoute_UnderTournamentsPath()
    {
        // Arrange
        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<AddTournamentSponsorCommand, Success>>(MockBehavior.Strict);
        var endpoint = Factory.Create<AddTournamentSponsorEndpoint>(commandHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("POST");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("tournaments"), "should be under the /tournaments path");
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
    }
}
