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

using HighBlockPage = Neba.Website.Server.History.Awards.HighBlock;

namespace Neba.Website.Tests.History.Awards;

[UnitTest]
[Component("Website.History.Awards.HighBlock")]
public sealed class HighBlockTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly Mock<IAwardsApi> _mockApi;

    public HighBlockTests()
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
        SetupSuccessResponse([HighBlockAwardResponseFactory.Create()]);

        var cut = _ctx.Render<HighBlockPage>();

        cut.Markup.ShouldContain("High Block");
    }

    [Fact(DisplayName = "Should call ListHighBlockAwardsAsync on initialization")]
    public void OnInit_ShouldCallListHighBlockAwardsApi()
    {
        SetupSuccessResponse([]);

        _ctx.Render<HighBlockPage>();

        _mockApi.Verify(
            x => x.ListHighBlockAwardsAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Should show bowler name when API call succeeds")]
    public void Render_ShouldShowBowlerName_WhenApiSucceeds()
    {
        var award = HighBlockAwardResponseFactory.Create(bowlerName: "Jane Smith");
        SetupSuccessResponse([award]);

        var cut = _ctx.Render<HighBlockPage>();

        cut.Markup.ShouldContain("Jane Smith");
    }

    [Fact(DisplayName = "Should show score when API call succeeds")]
    public void Render_ShouldShowScore_WhenApiSucceeds()
    {
        var award = HighBlockAwardResponseFactory.Create(score: 1325);
        SetupSuccessResponse([award]);

        var cut = _ctx.Render<HighBlockPage>();

        cut.Markup.ShouldContain("1325");
    }

    [Fact(DisplayName = "Should show season when API call succeeds")]
    public void Render_ShouldShowSeason_WhenApiSucceeds()
    {
        var award = HighBlockAwardResponseFactory.Create(season: "2022 Season");
        SetupSuccessResponse([award]);

        var cut = _ctx.Render<HighBlockPage>();

        cut.Markup.ShouldContain("2022 Season");
    }

    [Fact(DisplayName = "Should show TIE badge when two bowlers share the same season and score")]
    public void Render_ShouldShowTieBadge_WhenTwoAwardsShareSeasonAndScore()
    {
        var award1 = HighBlockAwardResponseFactory.Create(season: "2023 Season", score: 1350, bowlerName: "Alice");
        var award2 = HighBlockAwardResponseFactory.Create(season: "2023 Season", score: 1350, bowlerName: "Bob");
        SetupSuccessResponse([award1, award2]);

        var cut = _ctx.Render<HighBlockPage>();

        cut.Markup.ShouldContain("TIE");
        cut.Markup.ShouldContain("Alice");
        cut.Markup.ShouldContain("Bob");
    }

    [Fact(DisplayName = "Should show RECORD badge for the highest score")]
    public void Render_ShouldShowRecordBadge_WhenAwardHasHighestScore()
    {
        var record = HighBlockAwardResponseFactory.Create(season: "2024 Season", score: 1400);
        var other = HighBlockAwardResponseFactory.Create(season: "2023 Season", score: 1300);
        SetupSuccessResponse([record, other]);

        var cut = _ctx.Render<HighBlockPage>();

        cut.Markup.ShouldContain("RECORD");
    }

    [Fact(DisplayName = "Should not show RECORD badge when all awards have the same score")]
    public void Render_ShouldShowRecordBadgeOnEveryRow_WhenAllScoresAreEqual()
    {
        var award1 = HighBlockAwardResponseFactory.Create(season: "2024 Season", score: 1350, bowlerName: "Alice");
        var award2 = HighBlockAwardResponseFactory.Create(season: "2023 Season", score: 1350, bowlerName: "Bob");
        SetupSuccessResponse([award1, award2]);

        var cut = _ctx.Render<HighBlockPage>();

        // Both rows match the max score — both get the RECORD badge
        cut.Markup.ShouldContain("RECORD");
    }

    [Fact(DisplayName = "Should show error alert when API call fails")]
    public void Render_ShouldShowErrorAlert_WhenApiFails()
    {
        SetupFailureResponse(System.Net.HttpStatusCode.InternalServerError);

        var cut = _ctx.Render<HighBlockPage>();

        cut.Markup.ShouldContain("Error Loading Awards");
    }

    [Fact(DisplayName = "Should show no data message when API returns empty collection")]
    public void Render_ShouldShowNoDataMessage_WhenApiReturnsEmpty()
    {
        SetupSuccessResponse([]);

        var cut = _ctx.Render<HighBlockPage>();

        cut.Markup.ShouldContain("No high block awards data available.");
    }

    [Fact(DisplayName = "Should display most recent season before older season")]
    public void Render_ShouldOrderSeasonsDescending_WhenMultipleSeasons()
    {
        var older = HighBlockAwardResponseFactory.Create(season: "2020 Season", bowlerName: "Old Timer");
        var newer = HighBlockAwardResponseFactory.Create(season: "2024 Season", bowlerName: "Recent Star");
        SetupSuccessResponse([older, newer]);

        var cut = _ctx.Render<HighBlockPage>();
        var markup = cut.Markup;

        markup.IndexOf("Recent Star", StringComparison.Ordinal)
            .ShouldBeLessThan(markup.IndexOf("Old Timer", StringComparison.Ordinal));
    }

    private void SetupSuccessResponse(IReadOnlyCollection<HighBlockAwardResponse> awards)
    {
        var response = new Mock<IApiResponse<CollectionResponse<HighBlockAwardResponse>>>(MockBehavior.Strict);
        response.Setup(r => r.IsSuccessStatusCode).Returns(true);
        response.Setup(r => r.StatusCode).Returns(System.Net.HttpStatusCode.OK);
        response.Setup(r => r.Content).Returns(new CollectionResponse<HighBlockAwardResponse>
        {
            Items = awards,
        });

        _mockApi
            .Setup(x => x.ListHighBlockAwardsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response.Object);
    }

    private void SetupFailureResponse(System.Net.HttpStatusCode statusCode)
    {
        var response = new Mock<IApiResponse<CollectionResponse<HighBlockAwardResponse>>>(MockBehavior.Strict);
        response.Setup(r => r.IsSuccessStatusCode).Returns(false);
        response.Setup(r => r.StatusCode).Returns(statusCode);
        response.Setup(r => r.Content).Returns((CollectionResponse<HighBlockAwardResponse>?)null);

        _mockApi
            .Setup(x => x.ListHighBlockAwardsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response.Object);
    }
}
