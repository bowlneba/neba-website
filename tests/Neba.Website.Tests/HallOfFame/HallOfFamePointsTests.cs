using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.HallOfFame;
using Neba.Api.Contracts.HallOfFame.GetHallOfFamePointsDetail;
using Neba.Api.Contracts.HallOfFame.ListHallOfFamePoints;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.HallOfFame;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Notifications;
using Neba.Website.Server.Services;

using Refit;
using Refit.Testing;

using HallOfFamePointsPage = Neba.Website.Server.HallOfFame.HallOfFamePoints;

namespace Neba.Website.Tests.HallOfFame;

[UnitTest]
[Component("Website.HallOfFame.HallOfFamePoints")]
public sealed class HallOfFamePointsTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly Mock<IHallOfFameApi> _mockApi;

    public HallOfFamePointsTests()
    {
        _mockApi = new Mock<IHallOfFameApi>(MockBehavior.Strict);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        _ctx.Services.AddSingleton(_mockApi.Object);
        _ctx.Services.AddSingleton(sp => new ApiExecutor(mockStopwatch.Object, sp.GetRequiredService<NavigationManager>(), NullLogger<ApiExecutor>.Instance));
    }

    public void Dispose()
        => _ctx.Dispose();

    [Fact(DisplayName = "Shows an eligible bowler under the eligible heading and a bowler without the criteria under not yet eligible")]
    public void Render_ShouldSplitEligibleFromNotYetEligible()
    {
        // Arrange
        SetupList(HallOfFamePointsResponseFactory.Create(standings:
        [
            HallOfFamePointsStandingResponseFactory.Create(bowlerName: "Ray Dunleavy", totalPoints: 42, eligible: true, meetsPoints: true),
            HallOfFamePointsStandingResponseFactory.Create(bowlerName: "Tom Davis", totalPoints: 12)
        ]));

        // Act
        var cut = _ctx.Render<HallOfFamePointsPage>();

        // Assert
        cut.Find(".pts-stage").TextContent.ShouldContain("Ray Dunleavy");
        cut.Find(".pts-stage").TextContent.ShouldNotContain("Tom Davis");
        cut.Find(".pts-below").TextContent.ShouldContain("Tom Davis");
    }

    [Fact(DisplayName = "Puts the not yet eligible rows in a scroll region so the rules stay within reach")]
    public void Render_ShouldPutNotYetEligibleRowsInScrollRegion()
    {
        // Arrange
        SetupList(HallOfFamePointsResponseFactory.Create(standings:
        [
            HallOfFamePointsStandingResponseFactory.Create(bowlerName: "Tom Davis"),
            HallOfFamePointsStandingResponseFactory.Create(bowlerId: "01JZ8K3M4N5P6Q7R8S9T0V1W3Y", bowlerName: "Ann Lee")
        ]));

        // Act
        var cut = _ctx.Render<HallOfFamePointsPage>();

        // Assert
        cut.Find(".pts-below .pts-scroll").QuerySelectorAll(".pts-row").Length.ShouldBe(2);
    }

    [Fact(DisplayName = "Omits the eligible section when nobody is eligible")]
    public void Render_ShouldOmitEligibleSection_WhenNobodyEligible()
    {
        // Arrange
        SetupList(HallOfFamePointsResponseFactory.Create(standings: [HallOfFamePointsStandingResponseFactory.Create()]));

        // Act
        var cut = _ctx.Render<HallOfFamePointsPage>();

        // Assert
        cut.FindAll(".pts-stage").ShouldBeEmpty();
        cut.FindAll(".pts-below").Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Shows what is open for a bowler with the points but too few seasons")]
    public void Render_ShouldShowNeedsMore_WhenPointsMetButSeasonsShort()
    {
        // Arrange
        SetupList(HallOfFamePointsResponseFactory.Create(standings:
        [
            HallOfFamePointsStandingResponseFactory.Create(
                bowlerName: "Ken Almeida", totalPoints: 42, seasonsWithPoints: 2, membershipYears: 15,
                eligible: false, meetsPoints: true, meetsSeasons: false)
        ]));

        // Act
        var cut = _ctx.Render<HallOfFamePointsPage>();

        // Assert
        var row = cut.Find(".pts-row");
        row.TextContent.ShouldContain("2 of 3 seasons");
        row.TextContent.ShouldContain("needs 1 more");
        row.QuerySelector(".pts-dot")!.ClassList.ShouldContain("pts-dot--met");
    }

    [Fact(DisplayName = "Says the count may be low, not needs more, when the membership history is incomplete")]
    public void Render_ShouldShowCountMayBeLow_WhenHistoryIncompleteAndUnderThreshold()
    {
        // Arrange
        SetupList(HallOfFamePointsResponseFactory.Create(standings:
        [
            HallOfFamePointsStandingResponseFactory.Create(
                bowlerName: "Walt Brennan", totalPoints: 40, seasonsWithPoints: 6, membershipYears: 8,
                membershipHistoryComplete: false, eligible: false, meetsPoints: true, meetsSeasons: true)
        ]));

        // Act
        var cut = _ctx.Render<HallOfFamePointsPage>();

        // Assert
        var row = cut.Find(".pts-row");
        row.TextContent.ShouldContain("8* of 10 years");
        row.TextContent.ShouldContain("count may be low");
        row.TextContent.ShouldNotContain("needs 2 more");
    }

    [Fact(DisplayName = "Says needs N more when the membership history is complete and under the threshold")]
    public void Render_ShouldShowNeedsMore_WhenHistoryCompleteAndUnderThreshold()
    {
        // Arrange
        SetupList(HallOfFamePointsResponseFactory.Create(standings:
        [
            HallOfFamePointsStandingResponseFactory.Create(
                bowlerName: "Sue Whitfield", totalPoints: 38, seasonsWithPoints: 6, membershipYears: 8,
                membershipHistoryComplete: true, eligible: false, meetsPoints: true, meetsSeasons: true)
        ]));

        // Act
        var cut = _ctx.Render<HallOfFamePointsPage>();

        // Assert
        cut.Find(".pts-row").TextContent.ShouldContain("needs 2 more");
    }

    [Fact(DisplayName = "Shows plain counts for a bowler under the points threshold")]
    public void Render_ShouldShowPlainCounts_WhenUnderPointsThreshold()
    {
        // Arrange
        SetupList(HallOfFamePointsResponseFactory.Create(standings:
        [
            HallOfFamePointsStandingResponseFactory.Create(
                bowlerName: "Tom Davis", totalPoints: 12, seasonsWithPoints: 2, membershipYears: 8, meetsSeasons: false)
        ]));

        // Act
        var cut = _ctx.Render<HallOfFamePointsPage>();

        // Assert
        var stats = cut.Find(".pts-stats").TextContent;
        stats.ShouldContain("2 seasons");
        stats.ShouldContain("8 years a member");
    }

    [Fact(DisplayName = "Shows the membership note and the Superior Performance note")]
    public void Render_ShouldShowBothNotes()
    {
        // Arrange
        SetupList(HallOfFamePointsResponseFactory.Create());

        // Act
        var cut = _ctx.Render<HallOfFamePointsPage>();

        // Assert
        var notes = cut.Find(".pts-notes").TextContent;
        notes.ShouldContain("Superior Performance only");
        notes.ShouldContain("count from 1999");
    }

    [Fact(DisplayName = "Shows the inducted line for a bowler in the Hall of Fame for another category")]
    public void Render_ShouldShowInductedLine_WhenInductedForOtherCategory()
    {
        // Arrange
        SetupList(HallOfFamePointsResponseFactory.Create(standings:
        [
            HallOfFamePointsStandingResponseFactory.Create(bowlerName: "Walt Brennan", inductedCategories: ["Meritorious Service"])
        ]));

        // Act
        var cut = _ctx.Render<HallOfFamePointsPage>();

        // Assert
        cut.Find(".pts-inducted").TextContent.ShouldBe("In the Hall of Fame for Meritorious Service");
    }

    [Fact(DisplayName = "Renders the rule groups from the response")]
    public void Render_ShouldRenderRuleGroups_FromResponse()
    {
        // Arrange
        SetupList(HallOfFamePointsResponseFactory.Create());

        // Act
        var cut = _ctx.Render<HallOfFamePointsPage>();

        // Assert
        cut.FindAll(".pts-rule-group").Count.ShouldBe(3);
        var rules = cut.FindAll(".pts-rule").Select(rule => rule.TextContent).ToList();
        rules.ShouldContain(text => text.Contains("12") && text.Contains("Bowler of the Year"));
        rules.ShouldContain(text => text.Contains('6') && text.Contains("Singles Title"));
        rules.ShouldContain(text => text.Contains('3') && text.Contains("NEBA High-Roller Title"));
    }

    [Fact(DisplayName = "Shows the empty message when nobody has points")]
    public void Render_ShouldShowEmptyMessage_WhenNoStandings()
    {
        // Arrange
        SetupList(HallOfFamePointsResponseFactory.Create(standings: []));

        // Act
        var cut = _ctx.Render<HallOfFamePointsPage>();

        // Assert
        cut.Find(".pts-empty").TextContent.ShouldContain("No bowlers have Hall of Fame points yet.");
    }

    [Fact(DisplayName = "Shows an error alert when the API fails")]
    public void Render_ShouldShowError_WhenApiFails()
    {
        // Arrange
        SetupListFailure(System.Net.HttpStatusCode.InternalServerError);

        // Act
        var cut = _ctx.Render<HallOfFamePointsPage>();

        // Assert
        cut.Markup.ShouldContain("Error Loading Hall of Fame Points");
        cut.FindAll(".pts-stage, .pts-below").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Attaches the search script once the list has rendered")]
    public void Render_ShouldAttachSearchModule_AfterListRenders()
    {
        // Arrange
        SetupList(HallOfFamePointsResponseFactory.Create());
        var module = _ctx.JSInterop.SetupModule("./HallOfFame/HallOfFamePoints.razor.js");
        module.SetupVoid("initialize", _ => true).SetVoidResult();

        // Act
        var cut = _ctx.Render<HallOfFamePointsPage>();
        cut.WaitForAssertion(() => cut.Find(".pts-tools").HasAttribute("hidden").ShouldBeFalse());

        // Assert
        module.VerifyInvoke("initialize");
    }

    [Fact(DisplayName = "Opens the points modal for the clicked bowler")]
    public void Click_ShouldOpenModal_ForTheClickedBowler()
    {
        // Arrange
        SetupList(HallOfFamePointsResponseFactory.Create(standings:
        [
            HallOfFamePointsStandingResponseFactory.Create(bowlerName: "Tom Davis")
        ]));
        SetupDetail(HallOfFamePointsStandingResponseFactory.ValidBowlerId);
        var cut = _ctx.Render<HallOfFamePointsPage>();

        // Act
        cut.Find(".pts-name").Click();

        // Assert
        _mockApi.Verify(
            x => x.GetHallOfFamePointsDetailAsync(HallOfFamePointsStandingResponseFactory.ValidBowlerId, It.IsAny<CancellationToken>()),
            Times.Once);
        cut.Find("[role='dialog']").TextContent.ShouldContain("Tom Davis");
    }

    private void SetupList(HallOfFamePointsResponse list)
    {
        using var response = new StubApiResponse<HallOfFamePointsResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = list
        };

        _mockApi
            .Setup(x => x.ListHallOfFamePointsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupListFailure(System.Net.HttpStatusCode statusCode)
    {
        using var response = new StubApiResponse<HallOfFamePointsResponse>
        {
            IsSuccessStatusCode = false,
            StatusCode = statusCode,
            Content = (HallOfFamePointsResponse?)null
        };

        _mockApi
            .Setup(x => x.ListHallOfFamePointsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupDetail(string bowlerId)
    {
        using var response = new StubApiResponse<HallOfFamePointsDetailResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = HallOfFamePointsDetailResponseFactory.Create(bowlerId: bowlerId)
        };

        _mockApi
            .Setup(x => x.GetHallOfFamePointsDetailAsync(bowlerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }
}
