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
        var tournament = TournamentSummaryViewModelFactory.Create();

        var cut = _ctx.Render<TournamentUpcomingCard>(parameters => parameters
            .Add(p => p.Tournament, tournament));

        cut.Markup.ShouldContain("Register");
        cut.Markup.ShouldContain("Registration open");
    }

    [Fact(DisplayName = "Should show registration later and sponsor fallback when registration and sponsor are missing")]
    public void Render_ShouldShowFallbackCopy_WhenOptionalDataMissing()
    {
        var tournament = TournamentSummaryViewModelFactory.Create() with
        {
            RegistrationUrl = null,
            Sponsor = null,
            BowlingCenterName = null,
            BowlingCenterCity = null,
            RegistrationStatus = null,
        };

        var cut = _ctx.Render<TournamentUpcomingCard>(parameters => parameters
            .Add(p => p.Tournament, tournament));

        cut.Markup.ShouldContain("Registration opens later");
        cut.Markup.ShouldContain("Sponsorship available");
        cut.Markup.ShouldContain("Host center to be announced");
    }
}
