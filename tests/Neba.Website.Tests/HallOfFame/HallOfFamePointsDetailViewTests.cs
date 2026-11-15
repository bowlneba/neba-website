using Bunit;

using Neba.Api.Contracts.HallOfFame;
using Neba.Api.Contracts.HallOfFame.GetHallOfFamePointsDetail;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.HallOfFame;
using Neba.Website.Server.HallOfFame;

namespace Neba.Website.Tests.HallOfFame;

[UnitTest]
[Component("Website.HallOfFame.HallOfFamePointsDetailView")]
public sealed class HallOfFamePointsDetailViewTests : IDisposable
{
    private static readonly HallOfFamePointRequirements Requirements = new()
    {
        Points = 36,
        Seasons = 3,
        MembershipYears = 10,
        MembershipDataStartYear = 1999
    };

    private readonly BunitContext _ctx = new();

    public void Dispose()
        => _ctx.Dispose();

    private IRenderedComponent<HallOfFamePointsDetailView> Render(
        HallOfFamePointsDetailLayout layout = HallOfFamePointsDetailLayout.Standing,
        HallOfFamePointRequirements? requirements = null,
        HallOfFamePointsDetailResponse? response = null)
        => _ctx.Render<HallOfFamePointsDetailView>(p => p
            .Add(x => x.Detail, (response ?? HallOfFamePointsDetailResponseFactory.Create()).ToViewModel())
            .Add(x => x.Layout, layout)
            .Add(x => x.Requirements, requirements));

    [Fact(DisplayName = "Shows the points table with running totals and the threshold row")]
    public void Render_ShouldShowTable_WithThresholdRow()
    {
        // Arrange
        var response = HallOfFamePointsDetailResponseFactory.Create(
            totalPoints: 42,
            breakdown:
            [
                new HallOfFamePointsEntryResponse { Source = "Singles Title", Date = new DateOnly(2018, 3, 1), Points = 30, RunningTotal = 30, ReachedThreshold = false },
                new HallOfFamePointsEntryResponse { Source = "Bowler of the Year", Date = new DateOnly(2019, 12, 31), Points = 12, RunningTotal = 42, ReachedThreshold = true }
            ]);

        // Act
        var cut = Render(response: response);

        // Assert
        cut.FindAll(".pm-table tbody tr").Count.ShouldBe(2);
        cut.FindAll(".pm-table tr.pm-reached").ShouldHaveSingleItem();
    }

    [Fact(DisplayName = "Standing layout orders the stats points, seasons, membership years")]
    public void Render_ShouldOrderStats_WhenStandingLayout()
    {
        // Arrange
        var response = HallOfFamePointsDetailResponseFactory.Create(totalPoints: 43, seasonsWithPoints: 8);

        // Act
        var cut = Render(response: response);

        // Assert
        cut.FindAll(".pm-stat span").Select(span => span.TextContent)
            .ShouldBe(["points", "seasons with points", "membership years"]);
    }

    [Fact(DisplayName = "Shows the thresholds only when the caller passes requirements")]
    public void Render_ShouldShowThresholds_OnlyWhenRequirementsPassed()
    {
        // Arrange
        // Act
        var with = Render(requirements: Requirements);
        var without = Render();

        // Assert
        with.Markup.ShouldContain("of 36 points");
        with.Markup.ShouldContain("(3 needed)");
        with.Markup.ShouldContain("(10 needed)");
        without.Markup.ShouldNotContain("of 36 points");
        without.Markup.ShouldNotContain("needed");
    }

    [Fact(DisplayName = "Shows the membership note and asterisk only when the history is incomplete")]
    public void Render_ShouldShowMembershipNote_WhenHistoryIncomplete()
    {
        // Arrange
        var incompleteResponse = HallOfFamePointsDetailResponseFactory.Create(membershipHistoryComplete: false);

        // Act
        var incomplete = Render(requirements: Requirements, response: incompleteResponse);
        var complete = Render();

        // Assert
        incomplete.Find(".pm-note").TextContent.ShouldContain("1999");
        incomplete.FindAll(".pm-stat b")[2].TextContent.ShouldEndWith("*");
        complete.FindAll(".pm-note").ShouldBeEmpty();
        complete.FindAll(".pm-stat b")[2].TextContent.ShouldNotContain("*");
    }

    [Fact(DisplayName = "Shows the empty messages when there are no points and no membership years")]
    public void Render_ShouldShowEmptyMessages_WhenNoData()
    {
        // Arrange
        var response = HallOfFamePointsDetailResponseFactory.Create(
            totalPoints: 0, seasonsWithPoints: 0, breakdown: [], membershipYears: []);

        // Act
        var cut = Render(response: response);

        // Assert
        cut.Markup.ShouldContain("No points yet.");
        cut.Markup.ShouldContain("Guest and non-member entries do not count.");
    }

    [Fact(DisplayName = "Profile layout lists year and title with no point values, total, or threshold row")]
    public void Render_ShouldShowPlainList_WhenProfileLayout()
    {
        // Arrange
        // Act
        var cut = Render(HallOfFamePointsDetailLayout.Profile);

        // Assert
        cut.FindAll(".pm-table").ShouldBeEmpty();
        cut.FindAll(".pm-reached").ShouldBeEmpty();
        cut.Markup.ShouldContain("Titles and awards");
        cut.Markup.ShouldNotContain("Points earned");
        var items = cut.FindAll(".pm-years")[0].QuerySelectorAll("li");
        items.Select(li => li.TextContent).ShouldBe(["2017Bowler of the Year", "2019Singles Title"]);
    }

    [Fact(DisplayName = "Profile layout orders the stats membership years, points, seasons")]
    public void Render_ShouldOrderStats_WhenProfileLayout()
    {
        // Arrange
        var response = HallOfFamePointsDetailResponseFactory.Create(totalPoints: 43, seasonsWithPoints: 8);

        // Act
        var cut = Render(HallOfFamePointsDetailLayout.Profile, response: response);

        // Assert
        cut.FindAll(".pm-stat span").Select(span => span.TextContent)
            .ShouldBe(["membership years", "points", "seasons with points"]);
    }
}