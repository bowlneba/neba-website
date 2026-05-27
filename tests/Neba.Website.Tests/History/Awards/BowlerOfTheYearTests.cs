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

using BowlerOfTheYearPage = Neba.Website.Server.History.Awards.BowlerOfTheYear;

namespace Neba.Website.Tests.History.Awards;

[UnitTest]
[Component("Website.History.Awards.BowlerOfTheYear")]
public sealed class BowlerOfTheYearTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly Mock<IAwardsApi> _mockApi;

    public BowlerOfTheYearTests()
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
        // Arrange
        SetupSuccessResponse([BowlerOfTheYearAwardResponseFactory.Create()]);

        // Act
        var cut = _ctx.Render<BowlerOfTheYearPage>();

        // Assert
        cut.Markup.ShouldContain("Bowler of the Year");
    }

    [Fact(DisplayName = "Should call ListBowlerOfTheYearAwardsAsync on initialization")]
    public void OnInit_ShouldCallListBowlerOfTheYearAwardsApi()
    {
        // Arrange
        SetupSuccessResponse([]);

        // Act
        _ctx.Render<BowlerOfTheYearPage>();

        // Assert
        _mockApi.Verify(
            x => x.ListBowlerOfTheYearAwardsAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Should show bowler name when API call succeeds")]
    public void Render_ShouldShowBowlerName_WhenApiSucceeds()
    {
        // Arrange
        var award = BowlerOfTheYearAwardResponseFactory.Create(bowlerName: "Jane Smith");
        SetupSuccessResponse([award]);

        // Act
        var cut = _ctx.Render<BowlerOfTheYearPage>();

        // Assert
        cut.Markup.ShouldContain("Jane Smith");
    }

    [Fact(DisplayName = "Should show season header when API call succeeds")]
    public void Render_ShouldShowSeasonHeader_WhenApiSucceeds()
    {
        // Arrange
        var award = BowlerOfTheYearAwardResponseFactory.Create(season: "2022 Season");
        SetupSuccessResponse([award]);

        // Act
        var cut = _ctx.Render<BowlerOfTheYearPage>();

        // Assert
        cut.Markup.ShouldContain("2022 Season");
    }

    [Fact(DisplayName = "Should display Open category as Bowler of the Year")]
    public void Render_ShouldDisplayOpenCategoryAsBotyLabel_WhenOpenCategory()
    {
        // Arrange
        var award = BowlerOfTheYearAwardResponseFactory.Create(category: "Open");
        SetupSuccessResponse([award]);

        // Act
        var cut = _ctx.Render<BowlerOfTheYearPage>();

        // Assert
        cut.Markup.ShouldContain("BOWLER OF THE YEAR");
    }

    [Fact(DisplayName = "Should display Senior category label when Senior award")]
    public void Render_ShouldDisplaySeniorLabel_WhenSeniorCategory()
    {
        // Arrange
        var award = BowlerOfTheYearAwardResponseFactory.Create(category: "Senior");
        SetupSuccessResponse([award]);

        // Act
        var cut = _ctx.Render<BowlerOfTheYearPage>();

        // Assert
        cut.Markup.ShouldContain("SENIOR");
    }

    [Fact(DisplayName = "Should show both bowlers when two categories exist in the same season")]
    public void Render_ShouldShowBothBowlers_WhenTwoCategoriesInSameSeason()
    {
        // Arrange
        var open = BowlerOfTheYearAwardResponseFactory.Create(season: "2024 Season", bowlerName: "Alice Adams", category: "Open");
        var senior = BowlerOfTheYearAwardResponseFactory.Create(season: "2024 Season", bowlerName: "Bob Baker", category: "Senior");
        SetupSuccessResponse([open, senior]);

        // Act
        var cut = _ctx.Render<BowlerOfTheYearPage>();

        // Assert
        cut.Markup.ShouldContain("Alice Adams");
        cut.Markup.ShouldContain("Bob Baker");
    }

    [Fact(DisplayName = "Should display most recent season card before older season card")]
    public void Render_ShouldOrderSeasonsDescending_WhenMultipleSeasons()
    {
        // Arrange
        var older = BowlerOfTheYearAwardResponseFactory.Create(season: "2020 Season", bowlerName: "Old Timer");
        var newer = BowlerOfTheYearAwardResponseFactory.Create(season: "2024 Season", bowlerName: "Recent Star");
        SetupSuccessResponse([older, newer]);

        // Act
        var cut = _ctx.Render<BowlerOfTheYearPage>();
        var markup = cut.Markup;

        // Assert
        markup.IndexOf("Recent Star", StringComparison.Ordinal)
            .ShouldBeLessThan(markup.IndexOf("Old Timer", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Should show error alert when API call fails")]
    public void Render_ShouldShowErrorAlert_WhenApiFails()
    {
        // Arrange
        SetupFailureResponse(System.Net.HttpStatusCode.InternalServerError);

        // Act
        var cut = _ctx.Render<BowlerOfTheYearPage>();

        // Assert
        cut.Markup.ShouldContain("Error Loading Awards");
    }

    [Fact(DisplayName = "Should show no data message when API returns empty collection")]
    public void Render_ShouldShowNoDataMessage_WhenApiReturnsEmpty()
    {
        // Arrange
        SetupSuccessResponse([]);

        // Act
        var cut = _ctx.Render<BowlerOfTheYearPage>();

        // Assert
        cut.Markup.ShouldContain("No awards data available.");
    }

    private void SetupSuccessResponse(IReadOnlyCollection<BowlerOfTheYearAwardResponse> awards)
    {
        var response = new Mock<IApiResponse<CollectionResponse<BowlerOfTheYearAwardResponse>>>(MockBehavior.Strict);
        response.Setup(r => r.IsSuccessStatusCode).Returns(true);
        response.Setup(r => r.StatusCode).Returns(System.Net.HttpStatusCode.OK);
        response.Setup(r => r.Content).Returns(new CollectionResponse<BowlerOfTheYearAwardResponse>
        {
            Items = awards,
        });

        _mockApi
            .Setup(x => x.ListBowlerOfTheYearAwardsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response.Object);
    }

    private void SetupFailureResponse(System.Net.HttpStatusCode statusCode)
    {
        var response = new Mock<IApiResponse<CollectionResponse<BowlerOfTheYearAwardResponse>>>(MockBehavior.Strict);
        response.Setup(r => r.IsSuccessStatusCode).Returns(false);
        response.Setup(r => r.StatusCode).Returns(statusCode);
        response.Setup(r => r.Content).Returns((CollectionResponse<BowlerOfTheYearAwardResponse>?)null);

        _mockApi
            .Setup(x => x.ListBowlerOfTheYearAwardsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response.Object);
    }
}