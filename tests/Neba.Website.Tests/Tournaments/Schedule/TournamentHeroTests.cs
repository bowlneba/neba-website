using Bunit;

using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;
using Neba.Website.Server.Tournaments.Schedule;

namespace Neba.Website.Tests.Tournaments.Schedule;

[UnitTest]
[Component("Website.Tournaments.Schedule.TournamentHero")]
public sealed class TournamentHeroTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render register CTA and status pill when registration is available")]
    public void Render_ShouldShowRegisterLink_WhenRegistrationUrlExists()
    {
        var tournament = SeasonTournamentViewModelFactory.Create();

        var cut = _ctx.Render<TournamentHero>(parameters => parameters
            .Add(p => p.Tournament, tournament));

        cut.Markup.ShouldContain("Register Now");
        cut.Markup.ShouldContain("Registration open");
        cut.Markup.ShouldContain(tournament.Name);
    }

    [Fact(DisplayName = "Should render disabled registration CTA when registration is unavailable")]
    public void Render_ShouldShowDisabledRegistration_WhenRegistrationUrlMissing()
    {
        var tournament = SeasonTournamentViewModelFactory.Create() with
        {
            RegistrationUrl = null,
            RegistrationStatus = null,
        };

        var cut = _ctx.Render<TournamentHero>(parameters => parameters
            .Add(p => p.Tournament, tournament));

        cut.Markup.ShouldContain("Registration opens soon");
    }

    [Fact(DisplayName = "Should show sponsorship available and host TBA fallbacks when data missing")]
    public void Render_ShouldShowFallbackCopy_WhenSponsorAndHostMissing()
    {
        var tournament = SeasonTournamentViewModelFactory.Create() with
        {
            Sponsor = null,
            BowlingCenterName = null,
            BowlingCenterCity = null,
        };

        var cut = _ctx.Render<TournamentHero>(parameters => parameters
            .Add(p => p.Tournament, tournament));

        cut.Markup.ShouldContain("Sponsorship available");
        cut.Markup.ShouldContain("To be announced");
    }
}