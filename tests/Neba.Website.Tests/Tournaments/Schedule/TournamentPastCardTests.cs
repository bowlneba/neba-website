using Bunit;

using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;
using Neba.Website.Server.Tournaments.Schedule;

namespace Neba.Website.Tests.Tournaments.Schedule;

[UnitTest]
[Component("Website.Tournaments.Schedule.TournamentPastCard")]
public sealed class TournamentPastCardTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render winners pill when winners are present")]
    public void Render_ShouldShowWinners_WhenWinnersExist()
    {
        var tournament = SeasonTournamentViewModelFactory.Create() with
        {
            Winners = ["Alex Example", "Jamie Sample"],
        };

        var cut = _ctx.Render<TournamentPastCard>(parameters => parameters
            .Add(p => p.Tournament, tournament));

        cut.Markup.ShouldContain("Alex Example / Jamie Sample");
        cut.Markup.ShouldContain("View results");
    }

    [Fact(DisplayName = "Should show results pending message when no winners are present")]
    public void Render_ShouldShowResultsPending_WhenWinnersMissing()
    {
        var tournament = SeasonTournamentViewModelFactory.Create() with
        {
            Winners = [],
        };

        var cut = _ctx.Render<TournamentPastCard>(parameters => parameters
            .Add(p => p.Tournament, tournament));

        cut.Markup.ShouldContain("Results pending");
    }

    [Fact(DisplayName = "Should link results link directly to tournament detail page without season segment")]
    public void Render_ShouldLinkResultsLink_ToTournamentDetailPage()
    {
        var tournament = SeasonTournamentViewModelFactory.Create(id: "01JSTX1234567890ABCDEFGHIJ");

        var cut = _ctx.Render<TournamentPastCard>(parameters => parameters
            .Add(p => p.Tournament, tournament));

        cut.Find(".tournament-past-card__results-link").GetAttribute("href")
            .ShouldBe("/tournaments/01JSTX1234567890ABCDEFGHIJ");
    }
}