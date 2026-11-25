using System.Net;

using Bunit;

using Neba.Api.Contracts.Series.GetSeriesAdvancement;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;
using Neba.Website.Server.Series;

namespace Neba.Website.Tests.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesAdvancersTests : IDisposable
{
    private readonly SeriesDetailHost _host = new();

    public void Dispose() => _host.Dispose();

    private void SetupPage()
        => _host.SetupDetailFor(SeriesAdvancementResponseFactory.Create(stages:
        [
            StageAdvancementResponseFactory.Create(stageId: "q1", order: 1),
            StageAdvancementResponseFactory.Create(stageId: "s1", order: 2, kind: "Survivor"),
        ]));

    [Fact(DisplayName = "Should show each settled Stage's names to an anonymous visitor, open, with no status badge or action button")]
    public void Advancers_ShouldShowNames_ForAnonymous()
    {
        // Arrange
        SetupPage();
        _host.SetupAdvancers(SeriesAdvancersResponseFactory.Create(
        [
            StageAdvancersResponseFactory.Create("q1", 1, "Qualifying",
            [
                AdvancerResponseFactory.Create("b1", "Dave Smith", place: 1),
                AdvancerResponseFactory.Create("b2", "Pat Plain", place: null, isBackfill: true, backfillPoints: 410),
            ]),
            StageAdvancersResponseFactory.Create("s1", 2, "Survivor",
                [AdvancerResponseFactory.Create("b3", "Mia Alpha", place: null, blockTotal: 1500)]),
        ]));

        // Act
        var cut = _host.Render();

        // Assert
        var results = cut.FindAll("[data-testid='series-stage-results']");
        results.Count.ShouldBe(2);
        results[0].TextContent.ShouldContain("Dave Smith");
        results[0].TextContent.ShouldContain("1st");
        results[0].TextContent.ShouldContain("Pat Plain");
        results[0].TextContent.ShouldContain("Bowler of the Year point list");
        results[0].TextContent.ShouldContain("410");
        results[0].TextContent.ShouldNotContain("Backfill");
        results[1].TextContent.ShouldContain("Mia Alpha");
        results[1].TextContent.ShouldContain("1500");
        cut.FindAll("[data-testid='stage-status']").ShouldBeEmpty();
        cut.FindAll("[data-testid='series-stage'] .series-act").ShouldBeEmpty();
        cut.Markup.ShouldNotContain("Official");
        cut.Markup.ShouldNotContain("Computed");
    }

    [Fact(DisplayName = "Should give the Qualifying table the phone layout class only when it shows the point list, and always give it to the Placement table")]
    public void Advancers_ShouldUsePhoneTableClass_WhenTableHasMoreThanThreeColumns()
    {
        // Arrange
        _host.SetupDetailFor(SeriesAdvancementResponseFactory.Create(stages:
        [
            StageAdvancementResponseFactory.Create(stageId: "q1", order: 1),
            StageAdvancementResponseFactory.Create(stageId: "q2", order: 2),
            StageAdvancementResponseFactory.Create(stageId: "p1", order: 3, kind: "Placement"),
        ]));
        _host.SetupAdvancers(SeriesAdvancersResponseFactory.Create(
        [
            StageAdvancersResponseFactory.Create("q1", 1, "Qualifying",
            [
                AdvancerResponseFactory.Create("b1", "Dave Smith", place: 1),
                AdvancerResponseFactory.Create("b2", "Pat Plain", place: null, isBackfill: true, backfillPoints: 410),
            ]),
            StageAdvancersResponseFactory.Create("q2", 2, "Qualifying",
                [AdvancerResponseFactory.Create("b3", "Mia Alpha", place: 1)]),
            StageAdvancersResponseFactory.Create("p1", 3, "Placement",
                [AdvancerResponseFactory.Create("b4", "Lee Park", place: 2, position: 1, bowled: true)]),
        ]));

        // Act
        var cut = _host.Render();

        // Assert
        var tables = cut.FindAll("[data-testid='series-stage-results'] table");
        tables.Count.ShouldBe(3);
        tables[0].ClassList.ShouldContain("neba-data-table");
        tables[1].ClassList.ShouldNotContain("neba-data-table");
        tables[2].ClassList.ShouldContain("neba-data-table");
    }

    [Fact(DisplayName = "Should group the tournaments by kind, one headed group for each kind present")]
    public void Advancers_ShouldGroupStagesByKind()
    {
        // Arrange
        SetupPage();

        // Act
        var cut = _host.Render();

        // Assert
        var groups = cut.FindAll("[data-testid='series-stage-group']");
        groups.Count.ShouldBe(2);
        groups[0].QuerySelector("h2")!.TextContent.ShouldContain(SeriesDisplay.GroupHeading("Qualifying"));
        groups[1].QuerySelector("h2")!.TextContent.ShouldContain(SeriesDisplay.GroupHeading("Survivor"));
        groups[0].QuerySelectorAll("[data-testid='series-stage']").Length.ShouldBe(1);
        groups[1].QuerySelectorAll("[data-testid='series-stage']").Length.ShouldBe(1);
    }

    [Fact(DisplayName = "Should name a Stage by its place in the season: the 2nd tournament, not Stage 2")]
    public void Advancers_ShouldNameStageByTournamentOrdinal()
    {
        // Arrange
        SetupPage();

        // Act
        var cut = _host.Render();

        // Assert
        var cards = cut.FindAll("[data-testid='series-stage']");
        cards[0].TextContent.ShouldContain("Qualifying · 1st tournament");
        cards[1].TextContent.ShouldContain("Survivor · 2nd tournament");
        cut.Markup.ShouldNotContain("· Stage");
    }

    [Fact(DisplayName = "Should collapse and reopen a Stage's results from its expander")]
    public void Advancers_ShouldToggleResults()
    {
        // Arrange
        SetupPage();
        _host.SetupAdvancers(SeriesAdvancersResponseFactory.Create(
            [StageAdvancersResponseFactory.Create("q1", 1, "Qualifying", [AdvancerResponseFactory.Create("b1", "Dave Smith")])]));
        var cut = _host.Render();
        var toggle = cut.Find("[data-testid='series-stage'][data-stage-id='q1'] [data-testid='series-stage-toggle']");
        toggle.GetAttribute("aria-expanded").ShouldBe("true");
        toggle.GetAttribute("aria-controls").ShouldBe("series-results-q1");

        // Act
        toggle.Click();

        // Assert
        cut.FindAll("[data-testid='series-stage-results']").ShouldBeEmpty();
        cut.Find("[data-testid='series-stage'][data-stage-id='q1'] [data-testid='series-stage-toggle']")
            .HasAttribute("aria-controls").ShouldBeFalse();
        cut.Find("[data-testid='series-stage'][data-stage-id='q1'] [data-testid='series-stage-toggle']")
            .GetAttribute("aria-expanded").ShouldBe("false");

        // Act
        cut.Find("[data-testid='series-stage'][data-stage-id='q1'] [data-testid='series-stage-toggle']").Click();

        // Assert
        cut.FindAll("[data-testid='series-stage-results']").Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Should give a Stage with no results no expander")]
    public void Advancers_ShouldHideExpander_WhenStageHasNoResults()
    {
        // Arrange
        SetupPage();
        _host.SetupAdvancers(SeriesAdvancersResponseFactory.Create(
            [StageAdvancersResponseFactory.Create("q1", 1, "Qualifying", [AdvancerResponseFactory.Create("b1", "Dave Smith")])]));

        // Act
        var cut = _host.Render();

        // Assert
        cut.FindAll("[data-testid='series-stage-toggle']").Count.ShouldBe(1);
        cut.FindAll("[data-testid='series-stage'][data-stage-id='s1'] [data-testid='series-stage-toggle']").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should list every Placement position, marking the finalists, not only the finalists")]
    public void Advancers_ShouldListEveryPlacementPosition()
    {
        // Arrange
        _host.SetupDetailFor(SeriesAdvancementResponseFactory.Create(stages:
            [StageAdvancementResponseFactory.Create(stageId: "p1", order: 1, kind: "Placement")]));
        _host.SetupAdvancers(SeriesAdvancersResponseFactory.Create(
        [
            StageAdvancersResponseFactory.Create("p1", 1, "Placement",
            [
                AdvancerResponseFactory.Create("b1", "Dave Smith", place: 2, advanced: true, position: 1, bowled: true, isFinalist: true),
                AdvancerResponseFactory.Create("b2", "Pat Plain", place: 9, advanced: false, position: 2, bowled: false, isFinalist: false),
            ]),
        ]));

        // Act
        var cut = _host.Render();

        // Assert
        var rows = cut.FindAll("[data-testid='series-stage-results'] [data-testid='series-advancer']");
        rows.Count.ShouldBe(2);
        rows[0].TextContent.ShouldContain("1st");
        rows[0].TextContent.ShouldContain("Dave Smith");
        rows[1].TextContent.ShouldContain("Pat Plain");
        rows[1].TextContent.ShouldContain("9th");
        cut.Markup.ShouldContain("Finalist");
    }

    [Fact(DisplayName = "Should leave out the results when no Stage is settled")]
    public void Advancers_ShouldHideResults_WhenNoStageSettled()
    {
        // Arrange
        SetupPage();

        // Act
        var cut = _host.Render();

        // Assert
        cut.FindAll("[data-testid='series-stage-results']").ShouldBeEmpty();
        cut.FindAll(".series-stage-list li").Count.ShouldBe(2);
    }

    [Fact(DisplayName = "Should leave out the results and keep the page when the advancers call fails")]
    public void Advancers_ShouldHideResults_WhenCallFails()
    {
        // Arrange
        SetupPage();
        _host.SetupAdvancersStatus(HttpStatusCode.InternalServerError);

        // Act
        var cut = _host.Render();

        // Assert
        cut.FindAll("[data-testid='series-stage-results']").ShouldBeEmpty();
        cut.FindAll(".series-stage-list li").Count.ShouldBe(2);
    }

    [Fact(DisplayName = "Should show the results to a signed-in user without Series.EditSeries")]
    public void Advancers_ShouldShow_WithoutEditSeries()
    {
        // Arrange
        SetupPage();
        _host.SignInWithoutEditSeries();
        _host.SetupAdvancers(SeriesAdvancersResponseFactory.Create(
            [StageAdvancersResponseFactory.Create("q1", 1, "Qualifying")]));

        // Act
        var cut = _host.Render();

        // Assert
        cut.FindAll("[data-testid='series-stage-results']").Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Should make no advancers call for a Series.EditSeries holder, who sees the season rail instead")]
    public void Advancers_ShouldNotLoad_ForEditSeriesHolder()
    {
        // Arrange
        _host.SetupDetailFor(SeriesAdvancementResponseFactory.Create());
        _host.SetupAdvancement(SeriesAdvancementResponseFactory.Create());
        _host.SignInAsManager();

        // Act
        var cut = _host.Render();

        // Assert
        _host.Api.Verify(x => x.GetSeriesAdvancersAsync(SeriesDetailHost.SeriesId, It.IsAny<CancellationToken>()), Times.Never);
        cut.FindAll("[data-testid='series-stage-results']").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should map advancers to the view model in Stage order")]
    public void Mapping_ShouldOrderStages()
    {
        // Arrange
        var response = SeriesAdvancersResponseFactory.Create(
        [
            StageAdvancersResponseFactory.Create("b", 2),
            StageAdvancersResponseFactory.Create("a", 1),
        ]);

        // Act
        var model = response.ToViewModel();

        // Assert
        model.Select(s => s.Id).ShouldBe(["a", "b"]);
    }
}