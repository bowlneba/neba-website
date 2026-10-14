using Bunit;
using Bunit.TestDoubles;

using Neba.Api.Contracts.Security;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;
using Neba.Website.Server.Tournaments.Schedule;

namespace Neba.Website.Tests.Tournaments.Schedule;

[UnitTest]
[Component("Website.Tournaments.Schedule.TournamentHero")]
public sealed class TournamentHeroTests : IDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly BunitAuthorizationContext _authContext;

    public TournamentHeroTests()
    {
        _authContext = _ctx.AddAuthorization();
        _authContext.SetNotAuthorized();
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render the tournament type as a type badge")]
    public void Render_ShouldShowTypeBadge_WhenTournamentHasType()
    {
        // Arrange
        var tournament = SeasonTournamentViewModelFactory.Create(tournamentType: "Doubles");

        // Act
        var cut = _ctx.Render<TournamentHero>(parameters => parameters
            .Add(p => p.Tournament, tournament));

        // Assert
        var badge = cut.Find(".type-pill");
        badge.TextContent.Trim().ShouldBe("Doubles");
        badge.ClassList.ShouldContain("type-doubles");
    }

    [Fact(DisplayName = "Should render register CTA and status pill when registration is available")]
    public void Render_ShouldShowRegisterLink_WhenRegistrationUrlExists()
    {
        // Arrange
        var tournament = SeasonTournamentViewModelFactory.Create();

        // Act
        var cut = _ctx.Render<TournamentHero>(parameters => parameters
            .Add(p => p.Tournament, tournament));

        // Assert
        cut.Markup.ShouldContain("Register Now");
        cut.Markup.ShouldContain("Registration open");
        cut.Markup.ShouldContain(tournament.Name);
    }

    [Fact(DisplayName = "Should render disabled registration CTA when registration is unavailable")]
    public void Render_ShouldShowDisabledRegistration_WhenRegistrationUrlMissing()
    {
        // Arrange
        var tournament = SeasonTournamentViewModelFactory.Create() with
        {
            RegistrationUrl = null,
        };

        // Act
        var cut = _ctx.Render<TournamentHero>(parameters => parameters
            .Add(p => p.Tournament, tournament));

        // Assert
        cut.Markup.ShouldContain("Registration opens soon");
    }

    [Fact(DisplayName = "Should show sponsorship available and host TBA fallbacks when data missing")]
    public void Render_ShouldShowFallbackCopy_WhenSponsorAndHostMissing()
    {
        // Arrange
        var tournament = SeasonTournamentViewModelFactory.Create() with
        {
            Sponsor = null,
            BowlingCenterName = null,
            BowlingCenterCity = null,
        };

        // Act
        var cut = _ctx.Render<TournamentHero>(parameters => parameters
            .Add(p => p.Tournament, tournament));

        // Assert
        cut.Markup.ShouldContain("Sponsorship available");
        cut.Markup.ShouldContain("To be announced");
    }

    [Fact(DisplayName = "Should raise OnEditLogo with the tournament when the edit-logo button is clicked")]
    public async Task EditLogoButton_ShouldRaiseOnEditLogo_WhenClicked()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.CanEditTournamentLogoPolicyName);
        var tournament = SeasonTournamentViewModelFactory.Create();
        SeasonTournamentViewModel? raised = null;

        var cut = _ctx.Render<TournamentHero>(parameters => parameters
            .Add(p => p.Tournament, tournament)
            .Add(p => p.OnEditLogo, t => raised = t));

        // Act
        await cut.Find("button.tournament-logo-edit").ClickAsync();

        // Assert
        raised.ShouldBe(tournament);
    }

    [Fact(DisplayName = "Should not render the edit-logo button when the caller lacks the policy")]
    public void EditLogoButton_ShouldNotRender_WhenPolicyNotSatisfied()
    {
        // Arrange
        var tournament = SeasonTournamentViewModelFactory.Create();

        // Act
        var cut = _ctx.Render<TournamentHero>(parameters => parameters
            .Add(p => p.Tournament, tournament));

        // Assert
        cut.FindAll("button.tournament-logo-edit").ShouldBeEmpty();
    }
}