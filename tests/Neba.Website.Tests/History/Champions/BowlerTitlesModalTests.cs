using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.Bowlers;
using Neba.Api.Contracts.Bowlers.GetBowlerTitles;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Champions;
using Neba.Website.Server.Clock;
using Neba.Website.Server.History.Champions;
using Neba.Website.Server.Services;

using Refit;
using Refit.Testing;

namespace Neba.Website.Tests.History.Champions;

[UnitTest]
[Component("Website.History.Champions.BowlerTitlesModal")]
public sealed class BowlerTitlesModalTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly Mock<IBowlersApi> _mockBowlersApi;

    public BowlerTitlesModalTests()
    {
        _mockBowlersApi = new Mock<IBowlersApi>(MockBehavior.Strict);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        _ctx.Services.AddSingleton(new ApiExecutor(mockStopwatch.Object, NullLogger<ApiExecutor>.Instance));
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should not call API when modal is closed")]
    public void Render_ShouldNotCallApi_WhenModalIsClosed()
    {
        // Arrange & Act — no setup for GetBowlerTitlesAsync; Strict mock would throw on any call
        var cut = _ctx.Render<BowlerTitlesModal>(p => p
            .Add(m => m.IsOpen, false)
            .Add(m => m.OnClose, EventCallback.Empty)
            .Add(m => m.BowlersApi, _mockBowlersApi.Object)
            .Add(m => m.BowlerId, "bowler-1"));

        // Assert — no loading state shown and no exception thrown
        cut.Markup.ShouldNotContain("Loading title history");
    }

    [Fact(DisplayName = "Should call API with correct BowlerId when modal opens")]
    public void Render_ShouldCallApiWithBowlerId_WhenModalOpens()
    {
        // Arrange — setup with specific bowlerId; Strict mock enforces the correct arg is passed
        SetupSuccessResponse("bowler-42", BowlerTitlesViewModelFactory.ValidBowlerName, []);

        // Act
        var cut = _ctx.Render<BowlerTitlesModal>(p => p
            .Add(m => m.IsOpen, true)
            .Add(m => m.OnClose, EventCallback.Empty)
            .Add(m => m.BowlersApi, _mockBowlersApi.Object)
            .Add(m => m.BowlerId, "bowler-42"));

        // Assert — Data state reached means the API was called and returned successfully
        cut.Markup.ShouldNotContain("modal-state is-active");
    }

    [Fact(DisplayName = "Should show bowler name in header when data loads")]
    public void Render_ShouldShowBowlerName_WhenDataLoads()
    {
        // Arrange
        SetupSuccessResponse("b1", "Jane Smith", []);

        // Act
        var cut = _ctx.Render<BowlerTitlesModal>(p => p
            .Add(m => m.IsOpen, true)
            .Add(m => m.OnClose, EventCallback.Empty)
            .Add(m => m.BowlersApi, _mockBowlersApi.Object)
            .Add(m => m.BowlerId, "b1")
            .Add(m => m.BowlerName, "Jane Smith"));

        // Assert
        cut.Markup.ShouldContain("Jane Smith");
    }

    [Fact(DisplayName = "Should show title count in header")]
    public void Render_ShouldShowTitleCount_InHeader()
    {
        // Arrange
        SetupSuccessResponse("b1", "Joe", []);

        // Act
        var cut = _ctx.Render<BowlerTitlesModal>(p => p
            .Add(m => m.IsOpen, true)
            .Add(m => m.OnClose, EventCallback.Empty)
            .Add(m => m.BowlersApi, _mockBowlersApi.Object)
            .Add(m => m.BowlerId, "b1")
            .Add(m => m.BowlerName, "Joe")
            .Add(m => m.TitleCount, 7));

        // Assert
        cut.Markup.ShouldContain("7");
    }

    [Fact(DisplayName = "Should show HOF badge when HallOfFame is true")]
    public void Render_ShouldShowHofBadge_WhenHallOfFameIsTrue()
    {
        // Arrange
        SetupSuccessResponse("b1", "Joe", []);

        // Act
        var cut = _ctx.Render<BowlerTitlesModal>(p => p
            .Add(m => m.IsOpen, true)
            .Add(m => m.OnClose, EventCallback.Empty)
            .Add(m => m.BowlersApi, _mockBowlersApi.Object)
            .Add(m => m.BowlerId, "b1")
            .Add(m => m.HallOfFame, true));

        // Assert
        cut.Markup.ShouldContain("neba-hof.jpg");
    }

    [Fact(DisplayName = "Should not show HOF badge when HallOfFame is false")]
    public void Render_ShouldNotShowHofBadge_WhenHallOfFameIsFalse()
    {
        // Arrange
        SetupSuccessResponse("b1", "Joe", []);

        // Act
        var cut = _ctx.Render<BowlerTitlesModal>(p => p
            .Add(m => m.IsOpen, true)
            .Add(m => m.OnClose, EventCallback.Empty)
            .Add(m => m.BowlersApi, _mockBowlersApi.Object)
            .Add(m => m.BowlerId, "b1")
            .Add(m => m.HallOfFame, false));

        // Assert
        cut.Markup.ShouldNotContain("neba-hof.jpg");
    }

    [Fact(DisplayName = "Should show tournament hyperlink with correct href")]
    public void Render_ShouldShowTournamentHyperlink_WithCorrectHref()
    {
        // Arrange
        SetupSuccessResponse("b1", "Joe",
        [
            new BowlerTitleResponse
            {
                TournamentId   = "tourn-xyz",
                TournamentName = "Singles Classic",
                TournamentDate = new DateOnly(2024, 4, 1),
                TournamentType = TournamentType.Singles.Name,
            },
        ]);

        // Act
        var cut = _ctx.Render<BowlerTitlesModal>(p => p
            .Add(m => m.IsOpen, true)
            .Add(m => m.OnClose, EventCallback.Empty)
            .Add(m => m.BowlersApi, _mockBowlersApi.Object)
            .Add(m => m.BowlerId, "b1"));

        // Assert
        cut.Markup.ShouldContain("href=\"/tournaments/tourn-xyz\"");
        cut.Markup.ShouldContain("Singles Classic");
    }

    [Fact(DisplayName = "Should show formatted date in title row")]
    public void Render_ShouldShowFormattedDate_InTitleRow()
    {
        // Arrange
        SetupSuccessResponse("b1", "Joe",
        [
            new BowlerTitleResponse
            {
                TournamentId   = "t1",
                TournamentName = "Classic",
                TournamentDate = new DateOnly(2024, 4, 1),
                TournamentType = TournamentType.Singles.Name,
            },
        ]);

        // Act
        var cut = _ctx.Render<BowlerTitlesModal>(p => p
            .Add(m => m.IsOpen, true)
            .Add(m => m.OnClose, EventCallback.Empty)
            .Add(m => m.BowlersApi, _mockBowlersApi.Object)
            .Add(m => m.BowlerId, "b1"));

        // Assert
        cut.Markup.ShouldContain("Apr 2024");
    }

    [Fact(DisplayName = "Should show error state when API call fails")]
    public void Render_ShouldShowErrorState_WhenApiFails()
    {
        // Arrange
        SetupFailureResponse("b1", System.Net.HttpStatusCode.InternalServerError);

        // Act
        var cut = _ctx.Render<BowlerTitlesModal>(p => p
            .Add(m => m.IsOpen, true)
            .Add(m => m.OnClose, EventCallback.Empty)
            .Add(m => m.BowlersApi, _mockBowlersApi.Object)
            .Add(m => m.BowlerId, "b1"));

        // Assert
        cut.Markup.ShouldContain("Couldn't load titles");
        cut.Markup.ShouldContain("Retry");
    }

    [Fact(DisplayName = "Should not call API again when same BowlerId is re-rendered")]
    public void Render_ShouldNotCallApiAgain_WhenSameBowlerIdIsRendered()
    {
        // Arrange
        SetupSuccessResponse("b1", "Joe", []);
        var cut = _ctx.Render<BowlerTitlesModal>(p => p
            .Add(m => m.IsOpen, true)
            .Add(m => m.OnClose, EventCallback.Empty)
            .Add(m => m.BowlersApi, _mockBowlersApi.Object)
            .Add(m => m.BowlerId, "b1"));

        // Observe: component is in Data state after initial load
        cut.Markup.ShouldNotContain("modal-state is-active");

        // Act — re-render with same BowlerId; no new setup added, Strict mock would throw on unexpected call
        cut.Render(p => p
            .Add(m => m.IsOpen, true)
            .Add(m => m.OnClose, EventCallback.Empty)
            .Add(m => m.BowlersApi, _mockBowlersApi.Object)
            .Add(m => m.BowlerId, "b1"));

        // Assert — component still in Data state; no exception means deduplication guard held
        cut.Markup.ShouldNotContain("modal-state is-active");
    }

    [Fact(DisplayName = "Should call API again when BowlerId changes")]
    public void Render_ShouldCallApiAgain_WhenBowlerIdChanges()
    {
        // Arrange — setup for both bowlers; Strict mock enforces correct args on each call
        SetupSuccessResponse("b1", "Joe", []);
        SetupSuccessResponse("b2", "Jane", []);

        var cut = _ctx.Render<BowlerTitlesModal>(p => p
            .Add(m => m.IsOpen, true)
            .Add(m => m.OnClose, EventCallback.Empty)
            .Add(m => m.BowlersApi, _mockBowlersApi.Object)
            .Add(m => m.BowlerId, "b1"));

        // Act
        cut.Render(p => p
            .Add(m => m.IsOpen, true)
            .Add(m => m.OnClose, EventCallback.Empty)
            .Add(m => m.BowlersApi, _mockBowlersApi.Object)
            .Add(m => m.BowlerId, "b2"));

        // Assert — component is in Data state for the new bowler (not stuck in Loading)
        cut.Markup.ShouldNotContain("modal-state is-active");
    }

    private void SetupSuccessResponse(
        string bowlerId,
        string bowlerName,
        IReadOnlyCollection<BowlerTitleResponse> titles)
    {
        using var response = new StubApiResponse<BowlerTitlesResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new BowlerTitlesResponse
        {
            BowlerName = bowlerName,
            HallOfFame = false,
            Titles = titles,
        }
        };

        _mockBowlersApi
            .Setup(x => x.GetBowlerTitlesAsync(bowlerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupFailureResponse(string bowlerId, System.Net.HttpStatusCode statusCode)
    {
        using var response = new StubApiResponse<BowlerTitlesResponse>
        {
            IsSuccessStatusCode = false,
            StatusCode = statusCode,
            Content = (BowlerTitlesResponse?)null
        };

        _mockBowlersApi
            .Setup(x => x.GetBowlerTitlesAsync(bowlerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }
}