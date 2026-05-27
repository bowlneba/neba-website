using Bunit;

using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;
using Neba.Website.Server.Tournaments.Schedule;

namespace Neba.Website.Tests.Tournaments.Schedule;

[UnitTest]
[Component("Website.Tournaments.Schedule.TournamentUpcomingCard")]
public sealed class TournamentUpcomingCardTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

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
            RegistrationStatus = null,
        };

        // Act
        var cut = _ctx.Render<TournamentUpcomingCard>(parameters => parameters
            .Add(p => p.Tournament, tournament));

        // Assert
        cut.Markup.ShouldContain("Registration opens later");
        cut.Markup.ShouldContain("Sponsorship available");
        cut.Markup.ShouldContain("Host center to be announced");
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
}