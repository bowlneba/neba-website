using Bunit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Awards;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Awards;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Services;

using Refit;

using HighAveragePage = Neba.Website.Server.History.Awards.HighAverage;

namespace Neba.Website.Tests.History.Awards;

[UnitTest]
[Component("Website.History.Awards.HighAverage")]
public sealed class HighAverageTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly Mock<IAwardsApi> _mockApi;

    public HighAverageTests()
    {
        _mockApi = new Mock<IAwardsApi>(MockBehavior.Strict);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        _ctx.Services.AddSingleton(_mockApi.Object);
        _ctx.Services.AddSingleton(new ApiExecutor(mockStopwatch.Object, NullLogger<ApiExecutor>.Instance));
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render page title")]
    public void Render_ShouldShowPageTitle_WhenRendered()
    {
        SetupSuccessResponse([HighAverageAwardResponseFactory.Create()]);

        var cut = _ctx.Render<HighAveragePage>();

        cut.Markup.ShouldContain("High Average");
    }

    [Fact(DisplayName = "Should call ListHighAverageAwardsAsync on initialization")]
    public void OnInit_ShouldCallListHighAverageAwardsApi()
    {
        SetupSuccessResponse([]);

        _ctx.Render<HighAveragePage>();

        _mockApi.Verify(
            x => x.ListHighAverageAwardsAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Should show bowler name when API call succeeds")]
    public void Render_ShouldShowBowlerName_WhenApiSucceeds()
    {
        var award = HighAverageAwardResponseFactory.Create(bowlerName: "Jane Smith");
        SetupSuccessResponse([award]);

        var cut = _ctx.Render<HighAveragePage>();

        cut.Markup.ShouldContain("Jane Smith");
    }

    [Fact(DisplayName = "Should show formatted average when API call succeeds")]
    public void Render_ShouldShowFormattedAverage_WhenApiSucceeds()
    {
        var award = HighAverageAwardResponseFactory.Create(average: 228.75m);
        SetupSuccessResponse([award]);

        var cut = _ctx.Render<HighAveragePage>();

        cut.Markup.ShouldContain("228.75");
    }

    [Fact(DisplayName = "Should show season when API call succeeds")]
    public void Render_ShouldShowSeason_WhenApiSucceeds()
    {
        var award = HighAverageAwardResponseFactory.Create(season: "2022 Season");
        SetupSuccessResponse([award]);

        var cut = _ctx.Render<HighAveragePage>();

        cut.Markup.ShouldContain("2022 Season");
    }

    [Fact(DisplayName = "Should show game count when present")]
    public void Render_ShouldShowGameCount_WhenPresent()
    {
        var award = HighAverageAwardResponseFactory.Create(totalGames: 63);
        SetupSuccessResponse([award]);

        var cut = _ctx.Render<HighAveragePage>();

        cut.Markup.ShouldContain("63");
    }

    [Fact(DisplayName = "Should show em dash when game count is null")]
    public void Render_ShouldShowEmDash_WhenGameCountIsNull()
    {
        var award = HighAverageAwardResponseFactory.Create(totalGames: null);
        SetupSuccessResponse([award]);

        var cut = _ctx.Render<HighAveragePage>();

        cut.Markup.ShouldContain("—");
    }

    [Fact(DisplayName = "Should show tournament count when present")]
    public void Render_ShouldShowTournamentCount_WhenPresent()
    {
        var award = HighAverageAwardResponseFactory.Create(tournamentsParticipated: 14);
        SetupSuccessResponse([award]);

        var cut = _ctx.Render<HighAveragePage>();

        cut.Markup.ShouldContain("14");
    }

    [Fact(DisplayName = "Should show RECORD badge for the highest average")]
    public void Render_ShouldShowRecordBadge_WhenAwardHasHighestAverage()
    {
        var record = HighAverageAwardResponseFactory.Create(season: "2024 Season", average: 235.50m);
        var other = HighAverageAwardResponseFactory.Create(season: "2023 Season", average: 228.00m);
        SetupSuccessResponse([record, other]);

        var cut = _ctx.Render<HighAveragePage>();

        cut.Markup.ShouldContain("RECORD");
    }

    [Fact(DisplayName = "Should show error alert when API call fails")]
    public void Render_ShouldShowErrorAlert_WhenApiFails()
    {
        SetupFailureResponse(System.Net.HttpStatusCode.InternalServerError);

        var cut = _ctx.Render<HighAveragePage>();

        cut.Markup.ShouldContain("Error Loading Awards");
    }

    [Fact(DisplayName = "Should show no data message when API returns empty collection")]
    public void Render_ShouldShowNoDataMessage_WhenApiReturnsEmpty()
    {
        SetupSuccessResponse([]);

        var cut = _ctx.Render<HighAveragePage>();

        cut.Markup.ShouldContain("No high average awards data available.");
    }

    [Fact(DisplayName = "Should display most recent season before older season")]
    public void Render_ShouldOrderSeasonsDescending_WhenMultipleSeasons()
    {
        var older = HighAverageAwardResponseFactory.Create(season: "2020 Season", bowlerName: "Old Timer");
        var newer = HighAverageAwardResponseFactory.Create(season: "2024 Season", bowlerName: "Recent Star");
        SetupSuccessResponse([older, newer]);

        var cut = _ctx.Render<HighAveragePage>();
        var markup = cut.Markup;

        markup.IndexOf("Recent Star", StringComparison.Ordinal)
            .ShouldBeLessThan(markup.IndexOf("Old Timer", StringComparison.Ordinal));
    }

    private void SetupSuccessResponse(IReadOnlyCollection<HighAverageAwardResponse> awards)
    {
        var response = new Mock<IApiResponse<CollectionResponse<HighAverageAwardResponse>>>(MockBehavior.Strict);
        response.Setup(r => r.IsSuccessStatusCode).Returns(true);
        response.Setup(r => r.StatusCode).Returns(System.Net.HttpStatusCode.OK);
        response.Setup(r => r.Content).Returns(new CollectionResponse<HighAverageAwardResponse>
        {
            Items = awards,
        });

        _mockApi
            .Setup(x => x.ListHighAverageAwardsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response.Object);
    }

    private void SetupFailureResponse(System.Net.HttpStatusCode statusCode)
    {
        var response = new Mock<IApiResponse<CollectionResponse<HighAverageAwardResponse>>>(MockBehavior.Strict);
        response.Setup(r => r.IsSuccessStatusCode).Returns(false);
        response.Setup(r => r.StatusCode).Returns(statusCode);
        response.Setup(r => r.Content).Returns((CollectionResponse<HighAverageAwardResponse>?)null);

        _mockApi
            .Setup(x => x.ListHighAverageAwardsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response.Object);
    }
}
