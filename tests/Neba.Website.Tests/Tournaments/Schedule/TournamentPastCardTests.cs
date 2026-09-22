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
        // Arrange
        var tournament = SeasonTournamentViewModelFactory.Create() with
        {
            Winners = ["Alex Example", "Jamie Sample"],
        };

        // Act
        var cut = _ctx.Render<TournamentPastCard>(parameters => parameters
            .Add(p => p.Tournament, tournament));

        // Assert
        cut.Markup.ShouldContain("Alex Example / Jamie Sample");
        cut.Markup.ShouldContain("View results");
    }

    [Fact(DisplayName = "Should show results pending message when no winners are present")]
    public void Render_ShouldShowResultsPending_WhenWinnersMissing()
    {
        // Arrange
        var tournament = SeasonTournamentViewModelFactory.Create() with
        {
            Winners = [],
        };

        // Act
        var cut = _ctx.Render<TournamentPastCard>(parameters => parameters
            .Add(p => p.Tournament, tournament));

        // Assert
        cut.Markup.ShouldContain("Results pending");
    }

    [Fact(DisplayName = "Should not show a status pill for a completed tournament")]
    public void Render_ShouldNotShowStatusPill_WhenTournamentIsCompleted()
    {
        // Arrange
        var tournament = SeasonTournamentViewModelFactory.Create() with { Status = "Completed" };

        // Act
        var cut = _ctx.Render<TournamentPastCard>(parameters => parameters
            .Add(p => p.Tournament, tournament));

        // Assert
        cut.Markup.ShouldNotContain("tournament-past-card__status-pill");
    }

    [Fact(DisplayName = "Should show a Truncated status pill and name the winner without the champion trophy treatment")]
    public void Render_ShouldShowTruncatedPillAndPlainWinnerNote_WhenTournamentIsTruncated()
    {
        // Arrange — Truncated is never title eligible, regardless of entries.
        var tournament = SeasonTournamentViewModelFactory.Create() with
        {
            Status = "Truncated",
            TitleEligible = false,
            Winners = ["Alex Example"],
        };

        // Act
        var cut = _ctx.Render<TournamentPastCard>(parameters => parameters
            .Add(p => p.Tournament, tournament));

        // Assert
        var pill = cut.Find(".tournament-past-card__status-pill");
        pill.ClassList.ShouldContain("tournament-past-card__status-pill--truncated");
        pill.TextContent.ShouldContain("Truncated");
        cut.Markup.ShouldNotContain("tournament-past-card__champion-pill");
        cut.Markup.ShouldContain("Alex Example — not a title win");
    }

    [Fact(DisplayName = "Should show the champion trophy pill for a completed, title-eligible tournament")]
    public void Render_ShouldShowChampionPill_WhenTournamentIsCompletedAndTitleEligible()
    {
        // Arrange
        var tournament = SeasonTournamentViewModelFactory.Create() with
        {
            Status = "Completed",
            TitleEligible = true,
            Winners = ["Alex Example"],
        };

        // Act
        var cut = _ctx.Render<TournamentPastCard>(parameters => parameters
            .Add(p => p.Tournament, tournament));

        // Assert
        cut.Find(".tournament-past-card__champion-pill").ShouldNotBeNull();
        cut.Markup.ShouldContain("Alex Example");
    }

    [Fact(DisplayName = "Should not show the champion trophy pill for a completed tournament that fell short of the title minimum")]
    public void Render_ShouldNotShowChampionPill_WhenTournamentIsCompletedButNotTitleEligible()
    {
        // Arrange
        var tournament = SeasonTournamentViewModelFactory.Create() with
        {
            Status = "Completed",
            TitleEligible = false,
            Winners = ["Alex Example"],
        };

        // Act
        var cut = _ctx.Render<TournamentPastCard>(parameters => parameters
            .Add(p => p.Tournament, tournament));

        // Assert
        cut.Markup.ShouldNotContain("tournament-past-card__champion-pill");
        cut.Markup.ShouldContain("Alex Example — not a title win");
    }

    [Fact(DisplayName = "Should show a Cancelled status pill and no-official-results message instead of results pending")]
    public void Render_ShouldShowCancelledPillAndMessage_WhenTournamentIsCancelled()
    {
        // Arrange
        var tournament = SeasonTournamentViewModelFactory.Create() with
        {
            Status = "Cancelled",
            Winners = [],
        };

        // Act
        var cut = _ctx.Render<TournamentPastCard>(parameters => parameters
            .Add(p => p.Tournament, tournament));

        // Assert
        var pill = cut.Find(".tournament-past-card__status-pill");
        pill.ClassList.ShouldContain("tournament-past-card__status-pill--cancelled");
        pill.TextContent.ShouldContain("Cancelled");
        cut.Markup.ShouldContain("No official results — event cancelled");
        cut.Markup.ShouldNotContain("Results pending");
    }

    [Fact(DisplayName = "Should link results link directly to tournament detail page without season segment")]
    public void Render_ShouldLinkResultsLink_ToTournamentDetailPage()
    {
        // Arrange
        var tournament = SeasonTournamentViewModelFactory.Create(id: "01JSTX1234567890ABCDEFGHIJ");

        // Act
        var cut = _ctx.Render<TournamentPastCard>(parameters => parameters
            .Add(p => p.Tournament, tournament));

        // Assert
        cut.Find(".tournament-past-card__results-link").GetAttribute("href")
            .ShouldBe("/tournaments/01JSTX1234567890ABCDEFGHIJ");
    }
}