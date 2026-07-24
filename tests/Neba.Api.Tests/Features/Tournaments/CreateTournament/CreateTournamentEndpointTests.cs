using ErrorOr;

using FastEndpoints;

using Neba.Api.Contracts.Tournaments.CreateTournament;
using Neba.Api.Features.Tournaments;
using Neba.Api.Features.Tournaments.CreateTournament;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Features.Tournaments.CreateTournament;

[UnitTest]
[Component("Tournaments")]
public sealed class CreateTournamentEndpointTests
{
    [Fact(DisplayName = "HandleAsync should map request fields to command and take the success branch when creation succeeds")]
    public async Task HandleAsync_ShouldMapRequestToCommandAndTakeSuccessBranch_WhenCreationSucceeds()
    {
        // Arrange
        var input = TournamentInputFactory.Create(name: "NEBA Fall Classic");
        var request = new CreateTournamentRequest { Tournament = input };
        var ct = TestContext.Current.CancellationToken;
        var tournamentId = TournamentId.New();

        CreateTournamentCommand? capturedCommand = null;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CreateTournamentCommand, TournamentId>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<CreateTournamentCommand>(), ct))
            .Callback<CreateTournamentCommand, CancellationToken>((c, _) => capturedCommand = c)
            .ReturnsAsync(tournamentId);

        var endpoint = Factory.Create<CreateTournamentEndpoint>(commandHandlerMock.Object);

        // Act — Send.CreatedAtAsync requires LinkGenerator, which Factory.Create does not provide.
        // The strict mock verifies the command mapping; the LinkGenerator exception confirms the success branch was taken.
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => endpoint.HandleAsync(request, ct));

        // Assert
        exception.Message.ShouldContain("LinkGenerator");
        capturedCommand.ShouldNotBeNull();
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
        var input = TournamentInputFactory.Create(
            bowlingCenterCertificationNumber: "12345",
            logo: logo,
            oilPatternId: oilPatternId.Value.ToString());
        var request = new CreateTournamentRequest { Tournament = input };
        var ct = TestContext.Current.CancellationToken;

        CreateTournamentCommand? capturedCommand = null;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CreateTournamentCommand, TournamentId>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<CreateTournamentCommand>(), ct))
            .Callback<CreateTournamentCommand, CancellationToken>((c, _) => capturedCommand = c)
            .ReturnsAsync(TournamentId.New());

        var endpoint = Factory.Create<CreateTournamentEndpoint>(commandHandlerMock.Object);

        // Act
        await Should.ThrowAsync<InvalidOperationException>(
            () => endpoint.HandleAsync(request, ct));

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

    [Fact(DisplayName = "HandleAsync should map manually supplied pattern categories onto the command when no oil pattern is specified")]
    public async Task HandleAsync_ShouldMapManualPatternCategories_WhenNoOilPatternSpecified()
    {
        // Arrange
        var input = TournamentInputFactory.Create(
            patternLengthCategory: PatternLengthCategory.LongPattern.Name,
            patternRatioCategory: PatternRatioCategory.Sport.Name);
        var request = new CreateTournamentRequest { Tournament = input };
        var ct = TestContext.Current.CancellationToken;

        CreateTournamentCommand? capturedCommand = null;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CreateTournamentCommand, TournamentId>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<CreateTournamentCommand>(), ct))
            .Callback<CreateTournamentCommand, CancellationToken>((c, _) => capturedCommand = c)
            .ReturnsAsync(TournamentId.New());

        var endpoint = Factory.Create<CreateTournamentEndpoint>(commandHandlerMock.Object);

        // Act
        await Should.ThrowAsync<InvalidOperationException>(
            () => endpoint.HandleAsync(request, ct));

        // Assert
        capturedCommand.ShouldNotBeNull();
        capturedCommand.OilPatternId.ShouldBeNull();
        capturedCommand.PatternLengthCategory.ShouldBe(PatternLengthCategory.LongPattern);
        capturedCommand.PatternRatioCategory.ShouldBe(PatternRatioCategory.Sport);
    }

    [Fact(DisplayName = "HandleAsync should return 422 when the command returns a validation error")]
    public async Task HandleAsync_ShouldReturn422_WhenCommandReturnsValidationError()
    {
        // Arrange
        var request = new CreateTournamentRequest { Tournament = TournamentInputFactory.Create() };
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CreateTournamentCommand, TournamentId>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<CreateTournamentCommand>(), ct))
            .ReturnsAsync(TournamentErrors.NoSeasonForDates(TournamentInputFactory.ValidStartDate, TournamentInputFactory.ValidEndDate));

        var endpoint = Factory.Create<CreateTournamentEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(422);
    }

    [Fact(DisplayName = "Configure should register the CreateTournament permission policy on the tournaments route")]
    public void Configure_ShouldRegisterCreateTournamentPolicy_OnTournamentsRoute()
    {
        // Arrange
        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CreateTournamentCommand, TournamentId>>(MockBehavior.Strict);
        var endpoint = Factory.Create<CreateTournamentEndpoint>(commandHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("POST");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("tournaments"), "should be under the /tournaments path");
        endpoint.Definition.PreBuiltUserPolicies.ShouldNotBeNull();
        endpoint.Definition.PreBuiltUserPolicies.ShouldContain(Neba.Api.Contracts.Security.Permissions.CreateTournament.PolicyName);
    }
}