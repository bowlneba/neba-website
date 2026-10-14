using Bunit;
using Bunit.TestDoubles;

using Neba.Api.Contracts.Security;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;
using Neba.Website.Server.Tournaments.Schedule;

namespace Neba.Website.Tests.Tournaments.Schedule;

[UnitTest]
[Component("Website.Tournaments.Schedule.TournamentUpcomingCard")]
public sealed class TournamentUpcomingCardTests : IDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly BunitAuthorizationContext _authContext;

    public TournamentUpcomingCardTests()
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
        var cut = _ctx.Render<TournamentUpcomingCard>(parameters => parameters
            .Add(p => p.Tournament, tournament));

        // Assert
        var badge = cut.Find(".type-pill");
        badge.TextContent.Trim().ShouldBe("Doubles");
        badge.ClassList.ShouldContain("type-doubles");
    }

    [Fact(DisplayName = "Should render register action when registration URL exists")]
    public void Render_ShouldShowRegisterLink_WhenRegistrationAvailable()
    {
        // Arrange
        var tournament = SeasonTournamentViewModelFactory.Create();

        // Act
        var cut = _ctx.Render<TournamentUpcomingCard>(parameters => parameters
            .Add(p => p.Tournament, tournament));

        // Assert
        cut.Markup.ShouldContain("Register");
        cut.Markup.ShouldContain("Registration open");
    }

    [Theory(DisplayName = "Should render urgency tag with natural wording for imminent tournaments")]
    [InlineData(0, "Today")]
    [InlineData(1, "Tomorrow")]
    [InlineData(5, "In 5 days")]
    public void Render_ShouldShowUrgencyTag_WhenStartingWithinThreeWeeks(int days, string expected)
    {
        // Arrange
        var start = DateOnly.FromDateTime(DateTime.Today.AddDays(days));
        var tournament = SeasonTournamentViewModelFactory.Create(startDate: start, endDate: start);

        // Act
        var cut = _ctx.Render<TournamentUpcomingCard>(parameters => parameters
            .Add(p => p.Tournament, tournament));

        // Assert
        cut.Find(".tournament-upcoming-card__urgency-tag").TextContent.Trim().ShouldBe(expected);
    }

    [Fact(DisplayName = "Should show registration later and sponsor fallback when registration and sponsor are missing")]
    public void Render_ShouldShowFallbackCopy_WhenOptionalDataMissing()
    {
        // Arrange
        var tournament = SeasonTournamentViewModelFactory.Create() with
        {
            RegistrationUrl = null,
            Sponsor = null,
            BowlingCenterName = null,
            BowlingCenterCity = null,
        };

        // Act
        var cut = _ctx.Render<TournamentUpcomingCard>(parameters => parameters
            .Add(p => p.Tournament, tournament));

        // Assert
        cut.Markup.ShouldContain("Registration opens later");
        cut.Markup.ShouldContain("Sponsorship available");
        cut.Markup.ShouldContain("Host center to be announced");
    }

    [Fact(DisplayName = "Should show a Cancelled status pill and not-held note instead of price/register/capacity when cancelled")]
    public void Render_ShouldShowCancelledPillAndNote_WhenTournamentIsCancelled()
    {
        // Arrange
        var tournament = SeasonTournamentViewModelFactory.Create() with { Status = "Cancelled" };

        // Act
        var cut = _ctx.Render<TournamentUpcomingCard>(parameters => parameters
            .Add(p => p.Tournament, tournament));

        // Assert
        var pill = cut.Find(".tournament-upcoming-card__status-pill");
        pill.ClassList.ShouldContain("tournament-upcoming-card__status-pill--cancelled");
        pill.TextContent.ShouldContain("Cancelled");
        cut.Markup.ShouldContain("This event will not be held");
        cut.Markup.ShouldNotContain("Register");
        cut.Markup.ShouldNotContain("tournament-upcoming-card__capacity");
    }

    [Fact(DisplayName = "Should not show a status pill for a normally scheduled tournament")]
    public void Render_ShouldNotShowStatusPill_WhenTournamentIsScheduled()
    {
        // Arrange
        var tournament = SeasonTournamentViewModelFactory.Create() with { Status = "Scheduled" };

        // Act
        var cut = _ctx.Render<TournamentUpcomingCard>(parameters => parameters
            .Add(p => p.Tournament, tournament));

        // Assert
        cut.Markup.ShouldNotContain("tournament-upcoming-card__status-pill");
    }

    [Fact(DisplayName = "Should render View Details link pointing to tournament detail page")]
    public void Render_ShouldRenderViewDetailsLink_ToTournamentDetailPage()
    {
        // Arrange
        var tournament = SeasonTournamentViewModelFactory.Create(id: "01JSTX1234567890ABCDEFGHIJ");

        // Act
        var cut = _ctx.Render<TournamentUpcomingCard>(parameters => parameters
            .Add(p => p.Tournament, tournament));

        // Assert
        cut.Find(".tournament-upcoming-card__details-link").GetAttribute("href")
            .ShouldBe("/tournaments/01JSTX1234567890ABCDEFGHIJ");
    }

    [Fact(DisplayName = "Should raise OnEditLogo with the tournament when the edit-logo button is clicked")]
    public async Task EditLogoButton_ShouldRaiseOnEditLogo_WhenClicked()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.CanEditTournamentLogoPolicyName);
        var tournament = SeasonTournamentViewModelFactory.Create();
        SeasonTournamentViewModel? raised = null;

        var cut = _ctx.Render<TournamentUpcomingCard>(parameters => parameters
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
        var cut = _ctx.Render<TournamentUpcomingCard>(parameters => parameters
            .Add(p => p.Tournament, tournament));

        // Assert
        cut.FindAll("button.tournament-logo-edit").ShouldBeEmpty();
    }
}