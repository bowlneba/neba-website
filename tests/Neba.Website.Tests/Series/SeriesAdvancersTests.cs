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

    [Fact(DisplayName = "Should show each settled Stage's names to an anonymous visitor, with no status badge or action button")]
    public void Advancers_ShouldShowNames_ForAnonymous()
    {
        // Arrange
        SetupPage();
        _host.SetupAdvancers(SeriesAdvancersResponseFactory.Create(
        [
            StageAdvancersResponseFactory.Create("q1", 1, "Qualifying",
            [
                AdvancerResponseFactory.Create("b1", "Dave Smith", place: 1),
                AdvancerResponseFactory.Create("b2", "Pat Plain", place: null, isBackfill: true),
            ]),
            StageAdvancersResponseFactory.Create("s1", 2, "Survivor",
                [AdvancerResponseFactory.Create("b3", "Mia Alpha", place: null, blockTotal: 1500)]),
        ]));

        // Act
        var cut = _host.Render();

        // Assert
        var stages = cut.FindAll("[data-testid='series-advancers-stage']");
        stages.Count.ShouldBe(2);
        stages[0].TextContent.ShouldContain("Tournament 1");
        stages[0].TextContent.ShouldContain("Dave Smith");
        stages[0].TextContent.ShouldContain("1st");
        stages[0].TextContent.ShouldContain("Pat Plain");
        stages[0].TextContent.ShouldContain("Backfill");
        stages[1].TextContent.ShouldContain("Mia Alpha");
        stages[1].TextContent.ShouldContain("1500");
        cut.FindAll("[data-testid='stage-status']").ShouldBeEmpty();
        cut.FindAll("[data-testid='series-advancers'] button").ShouldBeEmpty();
        cut.Markup.ShouldNotContain("Official");
        cut.Markup.ShouldNotContain("Computed");
    }

    [Fact(DisplayName = "Should leave out the Who advanced section when no Stage is settled")]
    public void Advancers_ShouldHideSection_WhenNoStageSettled()
    {
        // Arrange
        SetupPage();

        // Act
        var cut = _host.Render();

        // Assert
        cut.FindAll("[data-testid='series-advancers']").ShouldBeEmpty();
        cut.FindAll(".series-stage-list li").Count.ShouldBe(2);
    }

    [Fact(DisplayName = "Should leave out the section and keep the page when the advancers call fails")]
    public void Advancers_ShouldHideSection_WhenCallFails()
    {
        // Arrange
        SetupPage();
        _host.SetupAdvancersStatus(HttpStatusCode.InternalServerError);

        // Act
        var cut = _host.Render();

        // Assert
        cut.FindAll("[data-testid='series-advancers']").ShouldBeEmpty();
        cut.FindAll(".series-stage-list li").Count.ShouldBe(2);
    }

    [Fact(DisplayName = "Should show the Who advanced section to a signed-in user without Series.EditSeries")]
    public void Advancers_ShouldShow_WithoutEditSeries()
    {
        // Arrange
        SetupPage();
        _host.SignInWithoutEditSeries();
        _host.SetupAdvancers(SeriesAdvancersResponseFactory.Create());

        // Act
        var cut = _host.Render();

        // Assert
        cut.FindAll("[data-testid='series-advancers-stage']").Count.ShouldBe(1);
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
        cut.FindAll("[data-testid='series-advancers']").ShouldBeEmpty();
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