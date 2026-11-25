using System.Net;

using Bunit;
using Bunit.TestDoubles;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts;
using Neba.Api.Contracts.BowlingCenters;
using Neba.Api.Contracts.BowlingCenters.ListBowlingCenters;
using Neba.Api.Contracts.Seasons;
using Neba.Api.Contracts.Seasons.ListTournamentsInSeason;
using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Series;
using Neba.Api.Contracts.Series.GetSeries;
using Neba.Api.Contracts.Series.GetSeriesAdvancement;
using Neba.Api.Contracts.Series.ListSeries;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Help;
using Neba.Website.Server.Notifications;
using Neba.Website.Server.Series;
using Neba.Website.Server.Services;

using Refit.Testing;

namespace Neba.Website.Tests.Series;

[UnitTest]
[Component("Series")]
public sealed class EditSeriesPageTests : IDisposable
{
    private const string SeriesId = "01JBQ5Y7K3M9N2P4R6T8V0W1X0";

    private readonly BunitContext _ctx = new();
    private readonly Mock<ISeriesApi> _seriesApi = new(MockBehavior.Strict);
    private readonly Mock<ISeasonsApi> _seasonsApi = new(MockBehavior.Strict);
    private readonly Mock<IBowlingCentersApi> _centersApi = new(MockBehavior.Strict);
    private readonly ToastService _toastService = new();
    private readonly List<IDisposable> _disposables = [];

    public EditSeriesPageTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.JSInterop.SetupModule("./Components/FileUpload.razor.js")
            .Setup<string?[]>("getPreviewUrls", _ => true).SetResult([]);
        var auth = _ctx.AddAuthorization();
        auth.SetAuthorized("test-user");
        auth.SetPolicies(Permissions.EditSeries.PolicyName);

        var stopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        stopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        stopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx.Services.AddSingleton(_seriesApi.Object);
        _ctx.Services.AddSingleton(_seasonsApi.Object);
        _ctx.Services.AddSingleton(_centersApi.Object);
        _ctx.Services.AddSingleton(sp => new ApiExecutor(stopwatch.Object, sp.GetRequiredService<NavigationManager>(), NullLogger<ApiExecutor>.Instance));
        _ctx.Services.AddSingleton(sp => new SeriesTournamentOptionsLoader(
            sp.GetRequiredService<ApiExecutor>(), _seasonsApi.Object, _seriesApi.Object));
        _ctx.Services.AddSingleton(_toastService);
        _ctx.Services.AddSingleton<HelpDocumentService>();

        SetupTournaments();
        SetupSeriesList();
        SetupCenters();
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _toastService.Dispose();
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
    }

    private StubApiResponse<T> Own<T>(StubApiResponse<T> response)
        where T : class
    {
        _disposables.Add(response);
        return response;
    }

    private StubApiResponse<T> Respond<T>(HttpStatusCode status, T? content)
        where T : class
        => Own(new StubApiResponse<T>
        {
            IsSuccessStatusCode = (int)status is >= 200 and < 300,
            StatusCode = status,
            Content = content,
        });

    private void SetupTournaments()
    {
        var response = Respond(HttpStatusCode.OK, new CollectionResponse<SeasonTournamentResponse> { Items = [] });
        _seasonsApi
            .Setup(x => x.ListTournamentsInSeasonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupSeriesList()
    {
        var response = Respond(HttpStatusCode.OK, new CollectionResponse<SeriesSummaryResponse> { Items = [] });
        _seriesApi.Setup(x => x.ListSeriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(response);
    }

    private void SetupCenters()
    {
        var response = Respond(HttpStatusCode.OK, new CollectionResponse<BowlingCenterSummaryResponse> { Items = [] });
        _centersApi.Setup(x => x.ListBowlingCentersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(response);
    }

    /// <summary>GetSeries returns three Stages (stage-1 to stage-3) on a Series that is locked or not.</summary>
    private void SetupDetail(bool isLocked)
    {
        var stages = new[]
        {
            SeriesStageResponseFactory.Create(id: "stage-1", order: 1, tournamentId: "t1"),
            SeriesStageResponseFactory.Create(id: "stage-2", order: 2, tournamentId: "t2"),
            SeriesStageResponseFactory.Create(id: "stage-3", order: 3, kind: "Placement", advancingCount: null, tournamentId: "t3"),
        };
        var response = Respond(HttpStatusCode.OK, SeriesDetailResponseFactory.Create(id: SeriesId, isLocked: isLocked, stages: stages));
        _seriesApi.Setup(x => x.GetSeriesAsync(SeriesId, It.IsAny<CancellationToken>())).ReturnsAsync(response);
    }

    private void SetupAdvancement(params StageAdvancementResponse[] stages)
    {
        var response = Respond(HttpStatusCode.OK, SeriesAdvancementResponseFactory.Create(stages: stages));
        _seriesApi.Setup(x => x.GetSeriesAdvancementAsync(SeriesId, It.IsAny<CancellationToken>())).ReturnsAsync(response);
    }

    private static StageAdvancementResponse Advancement(string stageId, int order, string status, string tournamentStatus)
        => StageAdvancementResponseFactory.Create(
            stageId: stageId, order: order, status: status, tournamentId: "t" + order, tournamentStatus: tournamentStatus);

    private async Task<IRenderedComponent<EditSeries>> RenderAsync()
    {
        var cut = _ctx.Render<EditSeries>(p => p.Add(x => x.Id, SeriesId));
        await cut.WaitForStateAsync(() => cut.FindAll("li.series-grid__row").Count > 0);
        return cut;
    }

    private static bool IsRemovable(IRenderedComponent<EditSeries> cut, int rowIndex)
        => cut.FindAll("li.series-grid__row")[rowIndex].QuerySelector("[data-testid='stage-remove-cancelled']") is not null;

    private void VerifyAdvancementCalls(Times times)
        => _seriesApi.Verify(x => x.GetSeriesAdvancementAsync(SeriesId, It.IsAny<CancellationToken>()), times);

    [Fact(DisplayName = "A locked Series should read the advancement once and let a Pending Stage with a cancelled tournament go")]
    public async Task Locked_ShouldCallAdvancementOnce_AndMarkPendingCancelledStageRemovable()
    {
        // Arrange
        SetupDetail(isLocked: true);
        SetupAdvancement(
            Advancement("stage-1", 1, "Official", "Completed"),
            Advancement("stage-2", 2, "Pending", "Cancelled"),
            Advancement("stage-3", 3, "Pending", "Scheduled"));

        // Act
        var cut = await RenderAsync();

        // Assert
        VerifyAdvancementCalls(Times.Once());
        IsRemovable(cut, 0).ShouldBeFalse();
        IsRemovable(cut, 1).ShouldBeTrue();
        IsRemovable(cut, 2).ShouldBeFalse();
        cut.Find("[data-testid='stage-cancelled-note']").TextContent.ShouldBe("Tournament cancelled: remove it from the Series");
        cut.Markup.ShouldContain("can still be removed");
        cut.FindAll("[data-testid='series-cancelled-check-warning']").ShouldBeEmpty();
    }

    [Theory(DisplayName = "A Stage should be removable only when it is Pending and its tournament is cancelled")]
    [InlineData("Pending", "Cancelled", true)]
    [InlineData("Pending", "Scheduled", false)]
    [InlineData("Pending", "Completed", false)]
    [InlineData("Computed", "Cancelled", false)]
    [InlineData("Official", "Cancelled", false)]
    public async Task Locked_ShouldMarkRemovable_OnlyWhenPendingAndCancelled(string status, string tournamentStatus, bool expected)
    {
        // Arrange
        SetupDetail(isLocked: true);
        SetupAdvancement(
            Advancement("stage-1", 1, "Computed", "Completed"),
            Advancement("stage-2", 2, status, tournamentStatus),
            Advancement("stage-3", 3, "Pending", "Scheduled"));

        // Act
        var cut = await RenderAsync();

        // Assert
        IsRemovable(cut, 1).ShouldBe(expected);
        cut.FindAll("[data-testid='stage-remove-cancelled']").Count.ShouldBe(expected ? 1 : 0);
    }

    [Fact(DisplayName = "The page should explain that the tournaments go in order and each has a role")]
    public async Task Render_ShouldShowTournamentOrderCopy()
    {
        // Arrange
        SetupDetail(isLocked: false);

        // Act
        var cut = await RenderAsync();

        // Assert
        cut.Find(".page-title-inner p").TextContent.ShouldBe("Set the tournaments in order. Each one has a role in the Series.");
    }

    [Fact(DisplayName = "A Stage missing from the advancement response should not be removable")]
    public async Task Locked_ShouldNotMarkRemovable_WhenStageMissingFromAdvancement()
    {
        // Arrange
        SetupDetail(isLocked: true);
        SetupAdvancement(
            Advancement("stage-2", 2, "Pending", "Cancelled"),
            Advancement("stage-other", 4, "Pending", "Cancelled"));

        // Act
        var cut = await RenderAsync();

        // Assert
        IsRemovable(cut, 0).ShouldBeFalse();
        IsRemovable(cut, 1).ShouldBeTrue();
        IsRemovable(cut, 2).ShouldBeFalse();
        cut.FindAll("[data-testid='stage-remove-cancelled']").Count.ShouldBe(1);
    }

    [Fact(DisplayName = "An unlocked Series should make no advancement call and show no removable Stage")]
    public async Task Unlocked_ShouldMakeNoAdvancementCall()
    {
        // Arrange
        SetupDetail(isLocked: false);

        // Act
        var cut = await RenderAsync();

        // Assert
        VerifyAdvancementCalls(Times.Never());
        cut.FindAll("[data-testid='stage-remove-cancelled']").ShouldBeEmpty();
        cut.FindAll("[data-testid='stage-cancelled-note']").ShouldBeEmpty();
        cut.FindAll("[data-testid='series-cancelled-check-warning']").ShouldBeEmpty();
    }

    [Fact(DisplayName = "A failed advancement read should warn and leave no Stage removable")]
    public async Task Locked_ShouldWarn_WhenAdvancementFails()
    {
        // Arrange
        SetupDetail(isLocked: true);
        var failure = Respond<SeriesAdvancementResponse>(HttpStatusCode.InternalServerError, null);
        _seriesApi.Setup(x => x.GetSeriesAdvancementAsync(SeriesId, It.IsAny<CancellationToken>())).ReturnsAsync(failure);

        // Act
        var cut = await RenderAsync();

        // Assert
        var warning = cut.Find("[data-testid='series-cancelled-check-warning']");
        warning.TextContent.ShouldContain("Cancelled tournaments not checked");
        warning.TextContent.ShouldContain("A cancelled tournament can't be removed until this check works. Reload the page to try again.");
        cut.FindAll("[data-testid='stage-remove-cancelled']").ShouldBeEmpty();
        cut.FindAll("li.series-grid__row").Count.ShouldBe(3);
        cut.Markup.ShouldNotContain("can still be removed");
        VerifyAdvancementCalls(Times.Once());
    }
}