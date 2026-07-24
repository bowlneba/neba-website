using ErrorOr;

using FastEndpoints;

using Neba.Api.Contracts.Tournaments.CreateTournament;
using Neba.Api.Contracts.Tournaments.EditTournament;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.EditTournament;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Features.Tournaments.EditTournament;

[UnitTest]
[Component("Tournaments")]
public sealed class EditTournamentEndpointTests
{
    [Fact(DisplayName = "HandleAsync should map request fields to command and take the success branch when edit succeeds")]
    public async Task HandleAsync_ShouldMapRequestToCommandAndTakeSuccessBranch_WhenEditSucceeds()
    {
        // Arrange
        var tournamentId = TournamentId.New();
        var input = EditTournamentInputFactory.Create(name: "Updated Tournament");
        var request = new EditTournamentRequest { Id = tournamentId.Value.ToString(), Tournament = input };
        var ct = TestContext.Current.CancellationToken;

        EditTournamentCommand? capturedCommand = null;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<EditTournamentCommand, Updated>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<EditTournamentCommand>(), ct))
            .Callback<EditTournamentCommand, CancellationToken>((c, _) => capturedCommand = c)
            .ReturnsAsync(Result.Updated);

        var endpoint = Factory.Create<EditTournamentEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        capturedCommand.ShouldNotBeNull();
        capturedCommand.TournamentId.ShouldBe(tournamentId);
        capturedCommand.Name.ShouldBe(input.Name);
        capturedCommand.TournamentType.ShouldBe(TournamentType.FromName(input.TournamentType));
        capturedCommand.StartDate.ShouldBe(input.StartDate);
        capturedCommand.EndDate.ShouldBe(input.EndDate);
        capturedCommand.StatsEligible.ShouldBe(input.StatsEligible);
        capturedCommand.EntryFee.ShouldBe(input.EntryFee);
        capturedCommand.NebaAddedMoney.ShouldBe(input.NebaAddedMoney);
        capturedCommand.BowlingCenterId.ShouldBeNull();
        capturedCommand.ExternalRegistrationUrl.ShouldBeNull();
        capturedCommand.Logo.ShouldBeNull();
        capturedCommand.OilPatternId.ShouldBeNull();
        capturedCommand.PatternLengthCategory.ShouldBeNull();
        capturedCommand.PatternRatioCategory.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should map a supplied bowling center, logo, and oil pattern onto the command")]
    public async Task HandleAsync_ShouldMapBowlingCenterLogoAndOilPattern_WhenSupplied()
    {
        // Arrange
        var logo = new TournamentLogoInput
        {
            Container = "tournament-logos",
            Path = "fall-classic/logo.png",
            ContentType = "image/png",
            SizeInBytes = 2048
        };
        var oilPatternId = OilPatternId.New();
        var input = EditTournamentInputFactory.Create(
            bowlingCenterCertificationNumber: "12345",
            logo: logo,
            oilPatternId: oilPatternId.Value.ToString());
        var request = new EditTournamentRequest { Id = TournamentId.New().Value.ToString(), Tournament = input };
        var ct = TestContext.Current.CancellationToken;

        EditTournamentCommand? capturedCommand = null;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<EditTournamentCommand, Updated>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<EditTournamentCommand>(), ct))
            .Callback<EditTournamentCommand, CancellationToken>((c, _) => capturedCommand = c)
            .ReturnsAsync(Result.Updated);

        var endpoint = Factory.Create<EditTournamentEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        capturedCommand.ShouldNotBeNull();
        capturedCommand.BowlingCenterId.ShouldNotBeNull();
        capturedCommand.BowlingCenterId.Value.ShouldBe("12345");

        capturedCommand.Logo.ShouldNotBeNull();
        capturedCommand.Logo.Container.ShouldBe(logo.Container);
        capturedCommand.Logo.Path.ShouldBe(logo.Path);
        capturedCommand.Logo.ContentType.ShouldBe(logo.ContentType);
        capturedCommand.Logo.SizeInBytes.ShouldBe(logo.SizeInBytes);

        capturedCommand.OilPatternId.ShouldBe(oilPatternId);
    }

    [Fact(DisplayName = "HandleAsync should return 404 when the command returns a not-found error")]
    public async Task HandleAsync_ShouldReturn404_WhenCommandReturnsNotFoundError()
    {
        // Arrange
        var request = new EditTournamentRequest
        {
            Id = TournamentId.New().Value.ToString(),
            Tournament = EditTournamentInputFactory.Create()
        };
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<EditTournamentCommand, Updated>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<EditTournamentCommand>(), ct))
            .ReturnsAsync(TournamentErrors.TournamentNotFound(TournamentId.New()));

        var endpoint = Factory.Create<EditTournamentEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }

    [Fact(DisplayName = "HandleAsync should return 422 when the command returns a validation error")]
    public async Task HandleAsync_ShouldReturn422_WhenCommandReturnsValidationError()
    {
        // Arrange
        var request = new EditTournamentRequest
        {
            Id = TournamentId.New().Value.ToString(),
            Tournament = EditTournamentInputFactory.Create()
        };
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<EditTournamentCommand, Updated>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<EditTournamentCommand>(), ct))
            .ReturnsAsync(TournamentErrors.NoSeasonForDates(EditTournamentInputFactory.ValidStartDate, EditTournamentInputFactory.ValidEndDate));

        var endpoint = Factory.Create<EditTournamentEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(422);
    }

    [Fact(DisplayName = "Configure should register the EditTournament permission policy on the tournaments route")]
    public void Configure_ShouldRegisterEditTournamentPolicy_OnTournamentsRoute()
    {
        // Arrange
        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<EditTournamentCommand, Updated>>(MockBehavior.Strict);
        var endpoint = Factory.Create<EditTournamentEndpoint>(commandHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("PUT");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("tournaments"), "should be under the /tournaments path");
        endpoint.Definition.PreBuiltUserPolicies.ShouldNotBeNull();
        endpoint.Definition.PreBuiltUserPolicies.ShouldContain(Neba.Api.Contracts.Security.Permissions.EditTournament.PolicyName);
    }
}