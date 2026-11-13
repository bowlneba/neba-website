using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.HallOfFame;
using Neba.Api.Contracts.HallOfFame.GetHallOfFamePointsDetail;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.HallOfFame;
using Neba.Website.Server.Clock;
using Neba.Website.Server.HallOfFame;
using Neba.Website.Server.Services;

using Refit;
using Refit.Testing;

namespace Neba.Website.Tests.HallOfFame;

[UnitTest]
[Component("Website.HallOfFame.HallOfFamePointsModal")]
public sealed class HallOfFamePointsModalTests : IDisposable
{
    private const string BowlerId = HallOfFamePointsDetailResponseFactory.ValidBowlerId;

    private readonly BunitContext _ctx;
    private readonly Mock<IHallOfFameApi> _mockApi;

    private static readonly HallOfFamePointRequirements Requirements = new()
    {
        Points = 36,
        Seasons = 3,
        MembershipYears = 10,
        MembershipDataStartYear = 1999
    };

    public HallOfFamePointsModalTests()
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

    private IRenderedComponent<HallOfFamePointsModal> RenderModal(
        bool isOpen = true,
        HallOfFamePointRequirements? requirements = null)
        => _ctx.Render<HallOfFamePointsModal>(parameters => parameters
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.BowlerId, BowlerId)
            .Add(p => p.BowlerName, "Jane Doe")
            .Add(p => p.OnClose, EventCallback.Empty)
            .Add(p => p.Requirements, requirements));

    [Fact(DisplayName = "Renders nothing and makes no API call while closed")]
    public void Render_ShouldRenderNothing_WhenClosed()
    {
        // Act
        var cut = RenderModal(isOpen: false);

        // Assert
        cut.Markup.Trim().ShouldBeEmpty();
    }

    [Fact(DisplayName = "Shows the loading state until the API answers")]
    public void Render_ShouldShowLoading_BeforeApiAnswers()
    {
        // Arrange
        var pending = new TaskCompletionSource<IApiResponse<HallOfFamePointsDetailResponse>>();
        _mockApi
            .Setup(x => x.GetHallOfFamePointsDetailAsync(BowlerId, It.IsAny<CancellationToken>()))
            .Returns(pending.Task);

        // Act
        var cut = RenderModal();

        // Assert
        cut.Markup.ShouldContain("Loading points");
        cut.Markup.ShouldNotContain("Points earned");
    }

    [Fact(DisplayName = "Shows the breakdown rows, membership years with type, and the total")]
    public void Render_ShouldShowBreakdownAndMembershipYears()
    {
        // Arrange
        SetupDetail(HallOfFamePointsDetailResponseFactory.Create(
            totalPoints: 18,
            breakdown:
            [
                new HallOfFamePointsEntryResponse { Source = "Bowler of the Year", Date = new DateOnly(2017, 12, 31), Points = 12, RunningTotal = 12, ReachedThreshold = false },
                new HallOfFamePointsEntryResponse { Source = "Singles Title", Date = new DateOnly(2019, 3, 10), Points = 6, RunningTotal = 18, ReachedThreshold = false }
            ],
            membershipYears:
            [
                new HallOfFameMembershipYearResponse { Year = 2024, MembershipType = "Renewal" },
                new HallOfFameMembershipYearResponse { Year = 2025, MembershipType = "Time Honored Member" }
            ]));

        // Act
        var cut = RenderModal();

        // Assert
        var rows = cut.FindAll(".pm-table tbody tr");
        rows.Count.ShouldBe(2);
        rows[0].TextContent.ShouldContain("2017");
        rows[0].TextContent.ShouldContain("Bowler of the Year");
        rows[1].TextContent.ShouldContain("Singles Title");
        cut.Find(".pm-table tfoot").TextContent.ShouldContain("18");

        var years = cut.FindAll(".pm-years li");
        years.Count.ShouldBe(2);
        years[1].TextContent.ShouldContain("2025");
        years[1].TextContent.ShouldContain("Time Honored Member");
    }

    private static HallOfFamePointsDetailResponse DetailWithThresholdEntry()
        => HallOfFamePointsDetailResponseFactory.Create(
            totalPoints: 42,
            breakdown:
            [
                new HallOfFamePointsEntryResponse { Source = "Singles Title", Date = new DateOnly(2018, 3, 1), Points = 30, RunningTotal = 30, ReachedThreshold = false },
                new HallOfFamePointsEntryResponse { Source = "Bowler of the Year", Date = new DateOnly(2019, 12, 31), Points = 12, RunningTotal = 42, ReachedThreshold = true },
                new HallOfFamePointsEntryResponse { Source = "Doubles Title", Date = new DateOnly(2024, 5, 2), Points = 6, RunningTotal = 48, ReachedThreshold = false }
            ]);

    [Fact(DisplayName = "Highlights the entry that took the bowler past the threshold and shows each running total")]
    public void Render_ShouldHighlightThresholdEntry()
    {
        // Arrange
        SetupDetail(DetailWithThresholdEntry());

        // Act
        var cut = RenderModal(requirements: Requirements);

        // Assert
        var highlighted = cut.FindAll(".pm-table tbody tr.pm-reached");
        var row = highlighted.ShouldHaveSingleItem();
        row.TextContent.ShouldContain("Bowler of the Year");
        row.QuerySelector(".pm-reached-tag")!.TextContent.ShouldBe("Reached 36 points");
        cut.FindAll(".pm-table tbody tr").Select(tr => tr.QuerySelectorAll("td")[3].TextContent)
            .ShouldBe(["30", "42", "48"]);
    }

    [Fact(DisplayName = "Uses a general label for the threshold entry when the caller passes no requirements")]
    public void Render_ShouldUseGeneralThresholdLabel_WhenRequirementsMissing()
    {
        // Arrange
        SetupDetail(DetailWithThresholdEntry());

        // Act
        var cut = RenderModal(requirements: null);

        // Assert
        cut.Find(".pm-reached-tag").TextContent.ShouldBe("Reached the points threshold");
    }

    [Fact(DisplayName = "Highlights nothing when no entry reached the threshold")]
    public void Render_ShouldHighlightNothing_WhenThresholdNotReached()
    {
        // Arrange
        SetupDetail(HallOfFamePointsDetailResponseFactory.Create());

        // Act
        var cut = RenderModal();

        // Assert
        cut.FindAll(".pm-reached").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Shows the thresholds when the caller passes requirements")]
    public void Render_ShouldShowThresholds_WhenRequirementsGiven()
    {
        // Arrange
        SetupDetail(HallOfFamePointsDetailResponseFactory.Create());

        // Act
        var cut = RenderModal(requirements: Requirements);

        // Assert
        cut.Markup.ShouldContain("of 36 points");
        cut.Markup.ShouldContain("(3 needed)");
        cut.Markup.ShouldContain("(10 needed)");
    }

    [Fact(DisplayName = "Drops the thresholds when the caller passes no requirements")]
    public void Render_ShouldDropThresholds_WhenRequirementsMissing()
    {
        // Arrange
        SetupDetail(HallOfFamePointsDetailResponseFactory.Create());

        // Act
        var cut = RenderModal(requirements: null);

        // Assert
        cut.Markup.ShouldNotContain("of 36 points");
        cut.Markup.ShouldNotContain("needed");
    }

    [Fact(DisplayName = "Shows the membership note only when the history is incomplete")]
    public void Render_ShouldShowMembershipNote_OnlyWhenHistoryIncomplete()
    {
        // Arrange
        SetupDetail(HallOfFamePointsDetailResponseFactory.Create(membershipHistoryComplete: false));

        // Act
        var incomplete = RenderModal(requirements: Requirements);

        // Assert
        incomplete.Find(".pm-note").TextContent.ShouldContain("first membership is not on file");
        incomplete.Find(".pm-note").TextContent.ShouldContain("1999");
    }

    [Fact(DisplayName = "Hides the membership note when the history is complete")]
    public void Render_ShouldHideMembershipNote_WhenHistoryComplete()
    {
        // Arrange
        SetupDetail(HallOfFamePointsDetailResponseFactory.Create(membershipHistoryComplete: true));

        // Act
        var cut = RenderModal();

        // Assert
        cut.FindAll(".pm-note").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Shows the inducted flag for an inducted bowler")]
    public void Render_ShouldShowInductedFlag_WhenInducted()
    {
        // Arrange
        SetupDetail(HallOfFamePointsDetailResponseFactory.Create(inductedCategories: ["Meritorious Service"]));

        // Act
        var cut = RenderModal();

        // Assert
        cut.Find(".pm-flag").TextContent.ShouldBe("In the Hall of Fame for Meritorious Service");
    }

    [Fact(DisplayName = "Shows no points and the guest message for a bowler with neither points nor membership years")]
    public void Render_ShouldShowEmptyMessages_WhenNoPointsAndNoMembershipYears()
    {
        // Arrange
        SetupDetail(HallOfFamePointsDetailResponseFactory.Create(
            totalPoints: 0, seasonsWithPoints: 0, breakdown: [], membershipYears: []));

        // Act
        var cut = RenderModal();

        // Assert
        cut.Markup.ShouldContain("No points yet.");
        cut.Markup.ShouldContain("Guest and non-member entries do not count.");
    }

    [Fact(DisplayName = "Shows an error with Retry when the API fails, and Retry loads the data")]
    public void Render_ShouldShowErrorAndRetry_WhenApiFails()
    {
        // Arrange
        SetupFailure(System.Net.HttpStatusCode.InternalServerError);
        var cut = RenderModal();
        cut.Markup.ShouldContain("Could not load points");

        SetupDetail(HallOfFamePointsDetailResponseFactory.Create(totalPoints: 18));

        // Act
        cut.Find("button.pm-retry").Click();

        // Assert
        cut.Markup.ShouldNotContain("Could not load points");
        cut.Find(".pm-table tfoot").TextContent.ShouldContain("18");
        _mockApi.Verify(x => x.GetHallOfFamePointsDetailAsync(BowlerId, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    private void SetupDetail(HallOfFamePointsDetailResponse detail)
    {
        using var response = new StubApiResponse<HallOfFamePointsDetailResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = detail
        };

        _mockApi
            .Setup(x => x.GetHallOfFamePointsDetailAsync(BowlerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupFailure(System.Net.HttpStatusCode statusCode)
    {
        using var response = new StubApiResponse<HallOfFamePointsDetailResponse>
        {
            IsSuccessStatusCode = false,
            StatusCode = statusCode,
            Content = (HallOfFamePointsDetailResponse?)null
        };

        _mockApi
            .Setup(x => x.GetHallOfFamePointsDetailAsync(BowlerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }
}
