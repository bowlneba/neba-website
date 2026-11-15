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
[Component("Website.HallOfFame.HallOfFamePointsDetailLoader")]
public sealed class HallOfFamePointsDetailLoaderTests : IDisposable
{
    private const string BowlerId = HallOfFamePointsDetailResponseFactory.ValidBowlerId;
    private const string OtherBowlerId = "01JZ8K3M4N5P6Q7R8S9T0V1W4D";

    private readonly BunitContext _ctx;
    private readonly Mock<IHallOfFameApi> _mockApi;

    public HallOfFamePointsDetailLoaderTests()
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

    [Fact(DisplayName = "Fetches the detail for the bowler id")]
    public void Render_ShouldFetchDetail_ForBowlerId()
    {
        // Arrange
        SetupDetail(BowlerId, HallOfFamePointsDetailResponseFactory.Create());

        // Act
        var cut = _ctx.Render<HallOfFamePointsDetailLoader>(p => p.Add(x => x.BowlerId, BowlerId));

        // Assert
        cut.FindAll(".pm-table").ShouldHaveSingleItem();
        _mockApi.Verify(x => x.GetHallOfFamePointsDetailAsync(BowlerId, It.IsAny<CancellationToken>()), Times.Once);
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
        var cut = _ctx.Render<HallOfFamePointsDetailLoader>(p => p.Add(x => x.BowlerId, BowlerId));

        // Assert
        cut.Markup.ShouldContain("Loading points");
        cut.Markup.ShouldNotContain("Points earned");
    }

    [Fact(DisplayName = "Shows an error with Retry when the API fails, and Retry loads the data")]
    public void Render_ShouldShowErrorAndRetry_WhenApiFails()
    {
        // Arrange
        SetupFailure(BowlerId);
        var cut = _ctx.Render<HallOfFamePointsDetailLoader>(p => p.Add(x => x.BowlerId, BowlerId));
        cut.Markup.ShouldContain("Could not load points");

        SetupDetail(BowlerId, HallOfFamePointsDetailResponseFactory.Create(totalPoints: 18));

        // Act
        cut.Find("button.pm-retry").Click();

        // Assert
        cut.Markup.ShouldNotContain("Could not load points");
        cut.Find(".pm-table tfoot").TextContent.ShouldContain("18");
        _mockApi.Verify(x => x.GetHallOfFamePointsDetailAsync(BowlerId, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact(DisplayName = "Fetches again when the bowler id changes")]
    public void SetParameters_ShouldRefetch_WhenBowlerIdChanges()
    {
        // Arrange
        SetupDetail(BowlerId, HallOfFamePointsDetailResponseFactory.Create());
        SetupDetail(OtherBowlerId, HallOfFamePointsDetailResponseFactory.Create(bowlerId: OtherBowlerId, totalPoints: 77));
        var cut = _ctx.Render<HallOfFamePointsDetailLoader>(p => p.Add(x => x.BowlerId, BowlerId));

        // Act
        cut.Render(p => p.Add(x => x.BowlerId, OtherBowlerId));

        // Assert
        cut.Find(".pm-table tfoot").TextContent.ShouldContain("77");
        _mockApi.Verify(x => x.GetHallOfFamePointsDetailAsync(OtherBowlerId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Ignores a slow response for a bowler id that has since changed")]
    public void SetParameters_ShouldIgnoreStaleResponse_WhenBowlerIdChangedMidLoad()
    {
        // Arrange
        var first = new TaskCompletionSource<IApiResponse<HallOfFamePointsDetailResponse>>();
        var second = new TaskCompletionSource<IApiResponse<HallOfFamePointsDetailResponse>>();
        _mockApi.Setup(x => x.GetHallOfFamePointsDetailAsync(BowlerId, It.IsAny<CancellationToken>())).Returns(first.Task);
        _mockApi.Setup(x => x.GetHallOfFamePointsDetailAsync(OtherBowlerId, It.IsAny<CancellationToken>())).Returns(second.Task);
        var cut = _ctx.Render<HallOfFamePointsDetailLoader>(p => p.Add(x => x.BowlerId, BowlerId));
        cut.Render(p => p.Add(x => x.BowlerId, OtherBowlerId));

        using var newer = new StubApiResponse<HallOfFamePointsDetailResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = HallOfFamePointsDetailResponseFactory.Create(bowlerId: OtherBowlerId, totalPoints: 77)
        };
        using var older = new StubApiResponse<HallOfFamePointsDetailResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = HallOfFamePointsDetailResponseFactory.Create(totalPoints: 18)
        };

        // Act
        second.SetResult(newer);
        cut.WaitForAssertion(() => cut.Find(".pm-table tfoot").TextContent.ShouldContain("77"));
        first.SetResult(older);

        // Assert
        cut.Find(".pm-table tfoot").TextContent.ShouldContain("77");
    }

    [Fact(DisplayName = "Uses a cached detail without calling the API and stores a fetched one")]
    public void Render_ShouldUseCache_WhenDetailIsCached()
    {
        // Arrange
        SetupDetail(BowlerId, HallOfFamePointsDetailResponseFactory.Create(totalPoints: 18));
        var cache = new Dictionary<string, HallOfFamePointsDetailViewModel>();
        var firstRender = _ctx.Render<HallOfFamePointsDetailLoader>(p => p
            .Add(x => x.BowlerId, BowlerId)
            .Add(x => x.Cache, cache));
        cache.ShouldContainKey(BowlerId);

        // Act
        var secondRender = _ctx.Render<HallOfFamePointsDetailLoader>(p => p
            .Add(x => x.BowlerId, BowlerId)
            .Add(x => x.Cache, cache));

        // Assert
        firstRender.Find(".pm-table tfoot").TextContent.ShouldContain("18");
        secondRender.Find(".pm-table tfoot").TextContent.ShouldContain("18");
        _mockApi.Verify(x => x.GetHallOfFamePointsDetailAsync(BowlerId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Raises OnLoaded with the detail after a successful load")]
    public void Render_ShouldRaiseOnLoaded_WhenLoaded()
    {
        // Arrange
        SetupDetail(BowlerId, HallOfFamePointsDetailResponseFactory.Create(inductedCategories: ["Meritorious Service"]));
        HallOfFamePointsDetailViewModel? loaded = null;

        // Act
        _ctx.Render<HallOfFamePointsDetailLoader>(p => p
            .Add(x => x.BowlerId, BowlerId)
            .Add(x => x.OnLoaded, EventCallback.Factory.Create<HallOfFamePointsDetailViewModel>(this, detail => loaded = detail)));

        // Assert
        loaded.ShouldNotBeNull();
        loaded.InductedLine.ShouldBe("In the Hall of Fame for Meritorious Service");
    }

    [Fact(DisplayName = "Renders EmptyContent when it is set and the bowler has no points and no membership years")]
    public void Render_ShouldRenderEmptyContent_WhenDetailIsEmpty()
    {
        // Arrange
        SetupDetail(BowlerId, HallOfFamePointsDetailResponseFactory.Create(
            totalPoints: 0, seasonsWithPoints: 0, breakdown: [], membershipYears: []));

        // Act
        var cut = _ctx.Render<HallOfFamePointsDetailLoader>(p => p
            .Add(x => x.BowlerId, BowlerId)
            .Add(x => x.EmptyContent, builder => builder.AddMarkupContent(0, "<p class=\"empty\">Nothing here</p>")));

        // Assert
        cut.Find(".empty").TextContent.ShouldBe("Nothing here");
        cut.FindAll(".pm-stat").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Renders the data view when EmptyContent is not set and the detail is empty")]
    public void Render_ShouldRenderDataView_WhenNoEmptyContent()
    {
        // Arrange
        SetupDetail(BowlerId, HallOfFamePointsDetailResponseFactory.Create(
            totalPoints: 0, seasonsWithPoints: 0, breakdown: [], membershipYears: []));

        // Act
        var cut = _ctx.Render<HallOfFamePointsDetailLoader>(p => p.Add(x => x.BowlerId, BowlerId));

        // Assert
        cut.Markup.ShouldContain("No points yet.");
    }

    [Fact(DisplayName = "Renders the data view when EmptyContent is set but the bowler has data")]
    public void Render_ShouldRenderDataView_WhenEmptyContentSetAndDataPresent()
    {
        // Arrange
        SetupDetail(BowlerId, HallOfFamePointsDetailResponseFactory.Create());

        // Act
        var cut = _ctx.Render<HallOfFamePointsDetailLoader>(p => p
            .Add(x => x.BowlerId, BowlerId)
            .Add(x => x.EmptyContent, builder => builder.AddMarkupContent(0, "<p class=\"empty\">Nothing here</p>")));

        // Assert
        cut.FindAll(".empty").ShouldBeEmpty();
        cut.FindAll(".pm-stat").Count.ShouldBe(3);
    }

    private void SetupDetail(string bowlerId, HallOfFamePointsDetailResponse detail)
    {
        using var response = new StubApiResponse<HallOfFamePointsDetailResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = detail
        };

        _mockApi
            .Setup(x => x.GetHallOfFamePointsDetailAsync(bowlerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupFailure(string bowlerId)
    {
        using var response = new StubApiResponse<HallOfFamePointsDetailResponse>
        {
            IsSuccessStatusCode = false,
            StatusCode = System.Net.HttpStatusCode.InternalServerError,
            Content = (HallOfFamePointsDetailResponse?)null
        };

        _mockApi
            .Setup(x => x.GetHallOfFamePointsDetailAsync(bowlerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }
}