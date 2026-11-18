using Bunit;
using Bunit.TestDoubles;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Series;
using Neba.Api.Contracts.Series.ListSeries;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Series;
using Neba.Website.Server.Services;
using Neba.Website.Server.Time;

using Refit;
using Refit.Testing;

namespace Neba.Website.Tests.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesListTests : IDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly BunitAuthorizationContext _authContext;
    private readonly Mock<ISeriesApi> _seriesApi = new(MockBehavior.Strict);
    private readonly List<IDisposable> _disposables = [];

    public SeriesListTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _authContext = _ctx.AddAuthorization();
        _authContext.SetNotAuthorized();

        var stopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        stopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        stopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        var timeZone = new Mock<IClientTimeZoneService>(MockBehavior.Strict);
        timeZone.Setup(x => x.ToLocalAsync(It.IsAny<DateTimeOffset>())).ReturnsAsync((DateTimeOffset utc) => utc);

        _ctx.Services.AddSingleton(_seriesApi.Object);
        _ctx.Services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero)));
        _ctx.Services.AddSingleton(timeZone.Object);
        _ctx.Services.AddSingleton(sp => new ApiExecutor(stopwatch.Object, sp.GetRequiredService<NavigationManager>(), NullLogger<ApiExecutor>.Instance));
    }

    public void Dispose()
    {
        _ctx.Dispose();
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
    }

    private void SetupList(params SeriesSummaryResponse[] series)
    {
        var response = new StubApiResponse<CollectionResponse<SeriesSummaryResponse>>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new CollectionResponse<SeriesSummaryResponse> { Items = series },
        };
        _disposables.Add(response);
        _seriesApi.Setup(x => x.ListSeriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(response);
    }

    private static SeriesSummaryResponse Current(string id = "current", bool locked = false, SeriesNextStageResponse? next = null)
        => SeriesSummaryResponseFactory.Create(id: id, name: "2025 Series", seasonEndDate: new DateOnly(2025, 12, 31), isLocked: locked, nextStage: next);

    private static SeriesSummaryResponse Past(string id = "past", bool locked = false)
        => SeriesSummaryResponseFactory.Create(
            id: id,
            name: "2024 Series",
            seasonStartDate: new DateOnly(2024, 1, 1),
            seasonEndDate: new DateOnly(2024, 12, 31),
            isLocked: locked);

    [Fact(DisplayName = "Should show the loading skeleton while the list loads")]
    public void Render_ShouldShowSkeleton_WhileLoading()
    {
        // Arrange
        _seriesApi
            .Setup(x => x.ListSeriesAsync(It.IsAny<CancellationToken>()))
            .Returns(new TaskCompletionSource<IApiResponse<CollectionResponse<SeriesSummaryResponse>>>().Task);

        // Act
        var cut = _ctx.Render<SeriesList>();

        // Assert
        cut.FindAll(".neba-skeleton").ShouldNotBeEmpty();
        cut.FindAll(".series-current").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should feature a Series whose season has not ended and list the rest as past")]
    public void Render_ShouldSplitCurrentAndPast()
    {
        // Arrange
        SetupList(Current(), Past());

        // Act
        var cut = _ctx.Render<SeriesList>();

        // Assert
        cut.FindAll("a.series-current").Count.ShouldBe(1);
        cut.Find("a.series-current").GetAttribute("href").ShouldBe("/series/current");
        cut.FindAll(".series-past li").Count.ShouldBe(1);
        cut.Find(".series-past a").GetAttribute("href").ShouldBe("/series/past");
        cut.Markup.ShouldContain("Past Series");
    }

    [Fact(DisplayName = "Should hide the Past Series heading when there are none")]
    public void Render_ShouldHidePastHeading_WhenNoPastSeries()
    {
        // Arrange
        SetupList(Current());

        // Act
        var cut = _ctx.Render<SeriesList>();

        // Assert
        cut.Markup.ShouldNotContain("Past Series");
    }

    [Fact(DisplayName = "Should show the next Stage only when the Series has one")]
    public void Render_ShouldShowNextStage_OnlyWhenSet()
    {
        // Arrange
        SetupList(
            Current("with-next", next: SeriesNextStageResponseFactory.Create()),
            Current("without-next"));

        // Act
        var cut = _ctx.Render<SeriesList>();

        // Assert
        cut.FindAll(".series-current__next").Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Should show the empty state when there are no Series")]
    public void Render_ShouldShowEmptyState()
    {
        // Arrange
        SetupList();

        // Act
        var cut = _ctx.Render<SeriesList>();

        // Assert
        cut.Markup.ShouldContain("No Series yet");
    }

    [Fact(DisplayName = "Should show an error when the list fails to load")]
    public void Render_ShouldShowError_WhenLoadFails()
    {
        // Arrange
        var response = new StubApiResponse<CollectionResponse<SeriesSummaryResponse>>
        {
            IsSuccessStatusCode = false,
            StatusCode = System.Net.HttpStatusCode.InternalServerError,
        };
        _disposables.Add(response);
        _seriesApi.Setup(x => x.ListSeriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(response);

        // Act
        var cut = _ctx.Render<SeriesList>();

        // Assert
        cut.Markup.ShouldContain("Unable to load Series");
    }

    [Fact(DisplayName = "Should show the create button only to a caller who can create a Series")]
    public void Render_ShouldShowCreateButton_OnlyWithPermission()
    {
        // Arrange
        SetupList(Current());

        // Act
        var without = _ctx.Render<SeriesList>();
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.CreateSeries.PolicyName);
        var with = _ctx.Render<SeriesList>();

        // Assert
        without.Markup.ShouldNotContain("/series/new");
        with.Markup.ShouldContain("/series/new");
    }

    [Fact(DisplayName = "Should show the Locked badge on a past Series only to a caller who can edit")]
    public void Render_ShouldShowLockedBadge_OnlyWithEditPermission()
    {
        // Arrange
        SetupList(Past(locked: true));

        // Act
        var without = _ctx.Render<SeriesList>();
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.EditSeries.PolicyName);
        var with = _ctx.Render<SeriesList>();

        // Assert
        without.FindAll(".series-locked-badge").ShouldBeEmpty();
        with.FindAll(".series-locked-badge").Count.ShouldBe(1);
    }
}
