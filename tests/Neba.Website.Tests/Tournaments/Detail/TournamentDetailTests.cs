using Bunit;
using Bunit.TestDoubles;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Sponsors;
using Neba.Api.Contracts.Tournaments;
using Neba.Api.Contracts.Tournaments.GetTournament;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Help;
using Neba.Website.Server.Notifications;
using Neba.Website.Server.Services;
using Neba.Website.Server.Time;
using Neba.Website.Server.Tournaments.Detail;

using Refit;
using Refit.Testing;

namespace Neba.Website.Tests.Tournaments.Detail;

[UnitTest]
[Component("Website.Tournaments.Detail.TournamentDetail")]
public sealed class TournamentDetailTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly Mock<ITournamentsApi> _mockApi;
    private readonly Mock<ISponsorsApi> _mockSponsorsApi;
    private readonly Mock<IClientTimeZoneService> _mockClientTimeZoneService;
    private readonly BunitAuthorizationContext _authContext;
    private readonly ToastService _toastService;

    public TournamentDetailTests()
    {
        _mockApi = new Mock<ITournamentsApi>(MockBehavior.Strict);
        _mockSponsorsApi = new Mock<ISponsorsApi>(MockBehavior.Strict);
        _mockClientTimeZoneService = new Mock<IClientTimeZoneService>(MockBehavior.Strict);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        _authContext = _ctx.AddAuthorization();
        _authContext.SetNotAuthorized();

        _toastService = new ToastService();

        _ctx.Services.AddSingleton(_mockApi.Object);
        _ctx.Services.AddSingleton(_mockSponsorsApi.Object);
        _ctx.Services.AddSingleton(_mockClientTimeZoneService.Object);
        _ctx.Services.AddSingleton(new ApiExecutor(mockStopwatch.Object, NullLogger<ApiExecutor>.Instance));
        _ctx.Services.AddSingleton(_toastService);
        _ctx.Services.AddSingleton<HelpDocumentService>();
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _toastService.Dispose();
    }

    // ── Loading state ────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should show loading spinner while API is pending")]
    public void Render_ShouldShowLoadingSpinner_WhileLoading()
    {
        // Arrange
        _mockApi
            .Setup(x => x.GetTournamentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(new TaskCompletionSource<IApiResponse<TournamentDetailResponse>>().Task);

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.Markup.ShouldContain("neba-spinner");
    }

    // ── Initialization ───────────────────────────────────────────────────────

    [Fact(DisplayName = "Should call GetTournamentAsync with the Id parameter")]
    public void OnInit_ShouldCallApiWithId()
    {
        // Arrange
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(id: TournamentDetailResponseFactory.ValidId));

        // Act
        _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        _mockApi.Verify(
            x => x.GetTournamentAsync(TournamentDetailResponseFactory.ValidId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Navigation on error ──────────────────────────────────────────────────

    [Fact(DisplayName = "Should navigate to /not-found when API call fails")]
    public void OnInit_ShouldNavigateToNotFound_WhenApiFails()
    {
        // Arrange
        SetupFailureResponse(System.Net.HttpStatusCode.NotFound);

        // Act
        _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        nav.Uri.ShouldEndWith("/not-found");
    }

    // ── Page title ───────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render page title with tournament name")]
    public void Render_ShouldRenderPageTitle_WithTournamentName()
    {
        // Arrange
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(name: "Spring Open"));

        // Act
        _ = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));
        var headOutlet = _ctx.Render<HeadOutlet>();

        // Assert
        headOutlet.Find("title").TextContent.ShouldBe("Spring Open - BowlNEBA");
    }

    // ── Header ───────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render tournament name in h1")]
    public void Render_ShouldRenderTournamentName_InHeading()
    {
        // Arrange
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(name: "Winter Classic"));

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.Find("h1").TextContent.ShouldContain("Winter Classic");
    }

    [Fact(DisplayName = "Should render tournament type chip")]
    public void Render_ShouldRenderTournamentTypeChip()
    {
        // Arrange
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            tournamentType: TournamentType.Doubles));

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.Markup.ShouldContain("Doubles");
    }

    [Fact(DisplayName = "Should render bowling center location when host is set")]
    public void Render_ShouldRenderLocation_WhenHostSet()
    {
        // Arrange
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            bowlingCenter: TournamentDetailBowlingCenterResponseFactory.Create(
                name: "Striker Lanes", city: "Manchester", state: "NH")));

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.Markup.ShouldContain("Striker Lanes");
        cut.Markup.ShouldContain("Manchester");
    }

    [Fact(DisplayName = "Should not render location when no bowling center is assigned")]
    public void Render_ShouldNotRenderLocation_WhenNoBowlingCenter()
    {
        // Arrange
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(bowlingCenter: null));

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.Find(".td-hero__eyebrow").TextContent.ShouldNotContain("Lanes");
    }

    [Fact(DisplayName = "Should render pattern length category chip when set")]
    public void Render_ShouldRenderPatternLengthChip_WhenSet()
    {
        // Arrange
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(patternLengthCategory: "Medium"));

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.Markup.ShouldContain("Medium");
    }

    [Fact(DisplayName = "Should not render pattern length chip when null")]
    public void Render_ShouldNotRenderPatternLengthChip_WhenNull()
    {
        // Arrange
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(patternLengthCategory: null));

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.FindAll(".td-hero__chips .td-hero__chip").Count.ShouldBe(1);
    }

    // ── Champion bar ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render champion bar when winners are present")]
    public void Render_ShouldRenderChampionBar_WhenWinnersPresent()
    {
        // Arrange
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            winners: ["Alex Example", "Jamie Sample"]));

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.Find(".tournament-detail__champion-bar").TextContent.ShouldContain("Alex Example / Jamie Sample");
    }

    [Fact(DisplayName = "Should not render champion bar when no winners")]
    public void Render_ShouldNotRenderChampionBar_WhenNoWinners()
    {
        // Arrange
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(winners: []));

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.FindAll(".tournament-detail__champion-bar").ShouldBeEmpty();
    }

    // ── Info card (upcoming) ─────────────────────────────────────────────────

    [Fact(DisplayName = "Should render info card with price and register link for upcoming tournament")]
    public void Render_ShouldRenderInfoCard_ForUpcomingTournament()
    {
        // Arrange
        var futureDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30));
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            startDate: futureDate,
            endDate: futureDate,
            entryFee: 95m,
            registrationUrl: new Uri("https://bowlneba.com/register"),
            addedMoney: 1500m));

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.FindAll(".td-rail-card").Count.ShouldBe(1);
        cut.Markup.ShouldContain("$95");
        cut.Markup.ShouldContain("$1,500");
        cut.Markup.ShouldContain("Register");
    }

    [Fact(DisplayName = "Should not render info card for past tournament")]
    public void Render_ShouldNotRenderInfoCard_ForPastTournament()
    {
        // Arrange
        var pastDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            startDate: pastDate,
            endDate: pastDate));

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.FindAll(".td-rail-card").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should not render register button when registration URL is absent")]
    public void Render_ShouldNotRenderRegisterButton_WhenNoRegistrationUrl()
    {
        // Arrange
        var futureDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30));
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            startDate: futureDate,
            endDate: futureDate,
            registrationUrl: null));

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.Markup.ShouldNotContain("Register");
    }

    // ── Entry count ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render entry count when present")]
    public void Render_ShouldRenderEntryCount_WhenPresent()
    {
        // Arrange
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(entryCount: 64));

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.Find(".td-rail-entries__value").TextContent.ShouldContain("64");
    }

    [Fact(DisplayName = "Should not render entry count when null")]
    public void Render_ShouldNotRenderEntryCount_WhenNull()
    {
        // Arrange
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(entryCount: null));

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.FindAll(".td-rail-entries").ShouldBeEmpty();
    }

    // ── Oil patterns ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render oil patterns section when patterns are present")]
    public void Render_ShouldRenderOilPatterns_WhenPresent()
    {
        // Arrange
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            oilPatterns: [
                TournamentDetailOilPatternResponseFactory.Create(name: "Kegel Broadway", length: 40, rounds: ["Qualifying", "Finals"]),
                TournamentDetailOilPatternResponseFactory.Create(name: "Kegel Crown", length: 39, rounds: ["Match Play"])]));

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.Markup.ShouldContain("Oil Pattern");
        cut.Markup.ShouldContain("Kegel Broadway · 40 ft");
        cut.Markup.ShouldContain("Qualifying, Finals");
    }

    [Theory(DisplayName = "Should render long dot class when pattern length is 43 or greater")]
    [InlineData(43)]
    [InlineData(44)]
    [InlineData(50)]
    public void Render_ShouldRenderLongDotClass_WhenLengthIsFortyThreeOrGreater(int length)
    {
        // Arrange
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            oilPatterns: [TournamentDetailOilPatternResponseFactory.Create(length: length)]));

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.Find(".td-pattern-card__dot--long").ShouldNotBeNull();
        cut.FindAll(".td-pattern-card__dot--medium").ShouldBeEmpty();
        cut.FindAll(".td-pattern-card__dot--short").ShouldBeEmpty();
    }

    [Theory(DisplayName = "Should render medium dot class when pattern length is between 38 and 42 inclusive")]
    [InlineData(38)]
    [InlineData(40)]
    [InlineData(42)]
    public void Render_ShouldRenderMediumDotClass_WhenLengthIsBetweenThirtyEightAndFortyTwo(int length)
    {
        // Arrange
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            oilPatterns: [TournamentDetailOilPatternResponseFactory.Create(length: length)]));

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.Find(".td-pattern-card__dot--medium").ShouldNotBeNull();
        cut.FindAll(".td-pattern-card__dot--long").ShouldBeEmpty();
        cut.FindAll(".td-pattern-card__dot--short").ShouldBeEmpty();
    }

    [Theory(DisplayName = "Should render short dot class when pattern length is 37 or less")]
    [InlineData(37)]
    [InlineData(35)]
    [InlineData(30)]
    public void Render_ShouldRenderShortDotClass_WhenLengthIsThirtySevenOrLess(int length)
    {
        // Arrange
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            oilPatterns: [TournamentDetailOilPatternResponseFactory.Create(length: length)]));

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.Find(".td-pattern-card__dot--short").ShouldNotBeNull();
        cut.FindAll(".td-pattern-card__dot--long").ShouldBeEmpty();
        cut.FindAll(".td-pattern-card__dot--medium").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should not render oil patterns section when no patterns")]
    public void Render_ShouldNotRenderOilPatterns_WhenEmpty()
    {
        // Arrange
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(oilPatterns: []));

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.Markup.ShouldNotContain("Oil Patterns");
    }

    [Fact(DisplayName = "Should render pending reveal note and full pattern detail when reveal date is set and full pattern info is present")]
    public void Render_ShouldShowPendingRevealNoteAndFullPatternDetail_WhenRevealDateSetAndFullPatternInfoPresent()
    {
        // Arrange — shape the API returns to a caller holding the tournament management permission pre-reveal.
        var revealUtc = DateTimeOffset.UtcNow.AddDays(7);
        var revealLocal = revealUtc.ToOffset(TimeSpan.FromHours(-5));
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            oilPatternRevealDateTime: revealUtc,
            oilPatterns: [TournamentDetailOilPatternResponseFactory.Create()]));
        _mockClientTimeZoneService.Setup(s => s.ToLocalAsync(revealUtc)).ReturnsAsync(revealLocal).Verifiable();

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.Markup.ShouldContain("Full details reveal to the public on");
        cut.Markup.ShouldContain("mL · ratio");
        _mockClientTimeZoneService.VerifyAll();
    }

    [Fact(DisplayName = "Should render only the category chip for an anonymous caller before the reveal date")]
    public void Render_ShouldShowCategoryChipOnly_WhenAnonymousBeforeReveal()
    {
        // Arrange — anonymous, pre-reveal shape: no OilPatternRevealDateTime, no OilPatterns, categories present.
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            patternLengthCategory: "Medium",
            patternRatioCategory: "Even",
            oilPatterns: []));

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.Markup.ShouldContain("Medium");
        cut.Markup.ShouldContain("Even");
        cut.FindAll(".td-pattern-card").ShouldBeEmpty();
        cut.Markup.ShouldNotContain("td-reveal-note");
    }

    [Fact(DisplayName = "Should render the revealed note once the reveal date has passed")]
    public void Render_ShouldShowRevealedNote_WhenRevealDateHasPassed()
    {
        // Arrange
        var revealUtc = DateTimeOffset.UtcNow.AddDays(-1);
        var revealLocal = revealUtc.ToOffset(TimeSpan.FromHours(-5));
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            oilPatternRevealDateTime: revealUtc,
            oilPatterns: [TournamentDetailOilPatternResponseFactory.Create()]));
        _mockClientTimeZoneService.Setup(s => s.ToLocalAsync(revealUtc)).ReturnsAsync(revealLocal).Verifiable();

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.Markup.ShouldContain("Revealed to the public");
        _mockClientTimeZoneService.VerifyAll();
    }

    [Fact(DisplayName = "Should not call the client time zone service when no reveal date is set")]
    public void Render_ShouldNotCallClientTimeZoneService_WhenNoRevealDateIsSet()
    {
        // Arrange — Strict mock with no setup: an unexpected call fails the test.
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            oilPatternRevealDateTime: null,
            oilPatterns: [TournamentDetailOilPatternResponseFactory.Create()]));

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.Markup.ShouldNotContain("td-reveal-note");
    }

    // ── Sponsors ─────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render the tournament's own logo image when a logo URL is present")]
    public void Render_ShouldRenderHeroLogoImage_WhenLogoUrlPresent()
    {
        // Arrange
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            logoUrl: new Uri("https://cdn.example.com/tournament-logo.png")));

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.Find(".td-hero__logo-img").GetAttribute("src").ShouldBe("https://cdn.example.com/tournament-logo.png");
    }

    [Fact(DisplayName = "Should render the format-specific default logo image when no logo URL is present")]
    public void Render_ShouldRenderHeroDefaultLogoImage_WhenNoLogoUrl()
    {
        // Arrange
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
tournamentType: TournamentType.Doubles, logoUrl: null));

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.Find(".td-hero__logo-img").GetAttribute("src").ShouldBe("/images/neba-doubles.jpg");
    }

    [Fact(DisplayName = "Should render sponsors section linking to sponsor detail pages")]
    public void Render_ShouldRenderSponsors_WithDetailLinks()
    {
        // Arrange
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            sponsors: [TournamentDetailSponsorResponseFactory.Create(
                name: "Acme Corp", slug: "acme-corp")]));

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.Markup.ShouldContain("Sponsors");
        cut.Markup.ShouldContain("/sponsors/acme-corp");
    }

    [Fact(DisplayName = "Should render sponsor logo image when logo URL is present")]
    public void Render_ShouldRenderSponsorLogo_WhenLogoUrlPresent()
    {
        // Arrange
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            sponsors: [TournamentDetailSponsorResponseFactory.Create(
                logoUrl: new Uri("https://cdn.example.com/acme-logo.png"))]));

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.Markup.ShouldContain("https://cdn.example.com/acme-logo.png");
        cut.FindAll(".td-rail-sponsor-card__name").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render sponsor name text when no logo URL")]
    public void Render_ShouldRenderSponsorName_WhenNoLogoUrl()
    {
        // Arrange
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            sponsors: [TournamentDetailSponsorResponseFactory.Create(
                name: "Acme Corp", logoUrl: null)]));

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.Find(".td-rail-sponsor-card__name").TextContent.ShouldBe("Acme Corp");
    }

    [Fact(DisplayName = "Should not render sponsors section when no sponsors")]
    public void Render_ShouldNotRenderSponsors_WhenEmpty()
    {
        // Arrange
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(sponsors: []));

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.FindAll(".td-rail-sponsors").ShouldBeEmpty();
    }

    // ── Results ──────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render results section with main cut bowler name")]
    public void Render_ShouldRenderResults_WithMainCutBowlerName()
    {
        // Arrange
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            results: [TournamentResultResponseFactory.Create(
                bowlerName: "Jane Smith", place: 1, sideCutName: null)]));

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.Markup.ShouldContain("Results");
        cut.Markup.ShouldContain("Jane Smith");
    }

    [Fact(DisplayName = "Should render side cut sections grouped by name")]
    public void Render_ShouldRenderSideCutSections_GroupedByName()
    {
        // Arrange
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            results:
            [
                TournamentResultResponseFactory.Create(bowlerName: "Jane Smith", sideCutName: "Senior"),
                TournamentResultResponseFactory.Create(bowlerName: "Bob Jones", sideCutName: "Senior"),
            ]));

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.FindAll(".tournament-detail__cut-section").Count.ShouldBe(1);
        cut.Find(".tournament-detail__cut-title").TextContent.Trim().ShouldBe("Senior");
        cut.Markup.ShouldContain("Jane Smith");
        cut.Markup.ShouldContain("Bob Jones");
    }

    [Fact(DisplayName = "Should show no-results message for past tournament without results")]
    public void Render_ShouldShowNoResultsMessage_WhenPastTournamentHasNoResults()
    {
        // Arrange
        var pastDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            startDate: pastDate,
            endDate: pastDate,
            results: []));

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.Find(".tournament-detail__no-results").TextContent.ShouldContain("Results are not yet available");
    }

    [Fact(DisplayName = "Should not show no-results message for upcoming tournament without results")]
    public void Render_ShouldNotShowNoResultsMessage_WhenUpcomingTournamentHasNoResults()
    {
        // Arrange
        var futureDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30));
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            startDate: futureDate,
            endDate: futureDate,
            results: []));

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.FindAll(".tournament-detail__no-results").ShouldBeEmpty();
    }

    // ── Manage Sponsors panel ────────────────────────────────────────────────

    [Fact(DisplayName = "Should not render Manage Sponsors panel when caller lacks permission")]
    public void Render_ShouldNotRenderManageSponsorsPanel_WhenNotAuthorized()
    {
        // Arrange
        SetupSuccessResponse(TournamentDetailResponseFactory.Create());

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.Markup.ShouldNotContain("Manage Sponsors");
    }

    [Fact(DisplayName = "Should render Manage Sponsors panel when caller has permission")]
    public void Render_ShouldRenderManageSponsorsPanel_WhenAuthorized()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.ManageTournamentSponsors.PolicyName);

        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            sponsors: [TournamentDetailSponsorResponseFactory.Create(
                name: "Acme Corp", titleSponsor: true, sponsorshipAmount: 2500m)]));

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.Markup.ShouldContain("Manage Sponsors");
        cut.Markup.ShouldContain("Title Sponsor");
        cut.Markup.ShouldContain("$2,500");
    }

    // ── Footer ───────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render back link to tournament schedule")]
    public void Render_ShouldRenderBackLink_ToTournamentSchedule()
    {
        // Arrange
        SetupSuccessResponse(TournamentDetailResponseFactory.Create());

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.Find(".tournament-detail__back-link").GetAttribute("href").ShouldBe("/tournaments");
    }

    // ── Delete tournament ────────────────────────────────────────────────────

    [Fact(DisplayName = "Should not show delete button when user lacks DeleteTournament permission")]
    public void Render_ShouldNotShowDeleteButton_WhenUserLacksPermission()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        SetupSuccessResponse(TournamentDetailResponseFactory.Create());

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.Markup.ShouldNotContain("td-hero__delete-btn");
    }

    [Fact(DisplayName = "Should show delete button when user has DeleteTournament permission")]
    public void Render_ShouldShowDeleteButton_WhenUserHasPermission()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.DeleteTournament.PolicyName);
        SetupSuccessResponse(TournamentDetailResponseFactory.Create());

        // Act
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Assert
        cut.Find("button.td-hero__delete-btn").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should open confirm dialog with tournament name when delete button is clicked")]
    public void Click_ShouldOpenConfirmDialog_WhenDeleteButtonIsClicked()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.DeleteTournament.PolicyName);
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(name: "NEBA Winter Championship"));
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        // Act
        cut.Find("button.td-hero__delete-btn").Click();

        // Assert
        cut.Markup.ShouldContain("Delete tournament?");
        cut.Markup.ShouldContain("NEBA Winter Championship");
    }

    [Fact(DisplayName = "Should close confirm dialog and stay on page when delete is cancelled")]
    public void CancelDelete_ShouldCloseDialogAndStayOnPage_WhenCancelled()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.DeleteTournament.PolicyName);
        SetupSuccessResponse(TournamentDetailResponseFactory.Create());
        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));
        cut.Find("button.td-hero__delete-btn").Click();

        // Act
        cut.Find("button.confirm-action-modal-cancel").Click();

        // Assert
        cut.Markup.ShouldNotContain("Delete tournament?");
        var navigationManager = _ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.Uri.ShouldNotEndWith("/tournaments");
    }

    [Fact(DisplayName = "Should navigate to /tournaments when delete succeeds")]
    public void ConfirmDelete_ShouldNavigateToTournaments_WhenDeleteSucceeds()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.DeleteTournament.PolicyName);
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(id: TournamentDetailResponseFactory.ValidId));

        using var deleteResponse = new StubApiResponse<object>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.NoContent
        };
        _mockApi
            .Setup(x => x.DeleteTournamentAsync(TournamentDetailResponseFactory.ValidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deleteResponse);

        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));
        cut.Find("button.td-hero__delete-btn").Click();

        // Act
        cut.Find("button.confirm-action-modal-confirm").Click();

        // Assert
        var navigationManager = _ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.Uri.ShouldEndWith("/tournaments");
        _toastService.Current.ShouldNotBeNull();
        _toastService.Current.Severity.ShouldBe(NotifySeverity.Success);
    }

    [Fact(DisplayName = "Should show error toast and stay on the page when delete is blocked by historical records")]
    public void ConfirmDelete_ShouldShowErrorToastAndStayOnPage_WhenDeleteFails()
    {
        // Arrange
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.DeleteTournament.PolicyName);
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(id: TournamentDetailResponseFactory.ValidId, name: "NEBA Winter Championship"));

        using var deleteResponse = new StubApiResponse<object>
        {
            IsSuccessStatusCode = false,
            StatusCode = System.Net.HttpStatusCode.Conflict
        };
        _mockApi
            .Setup(x => x.DeleteTournamentAsync(TournamentDetailResponseFactory.ValidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deleteResponse);

        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));
        cut.Find("button.td-hero__delete-btn").Click();

        // Act
        cut.Find("button.confirm-action-modal-confirm").Click();

        // Assert
        cut.Markup.ShouldNotContain("Delete tournament?");
        var navigationManager = _ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.Uri.ShouldNotEndWith("/tournaments");
        _toastService.Current.ShouldNotBeNull();
        _toastService.Current.Severity.ShouldBe(NotifySeverity.Error);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void SetupSuccessResponse(TournamentDetailResponse tournament)
    {
        using var response = new StubApiResponse<TournamentDetailResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = tournament
        };

        _mockApi
            .Setup(x => x.GetTournamentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupFailureResponse(System.Net.HttpStatusCode statusCode)
    {
        using var response = new StubApiResponse<TournamentDetailResponse>
        {
            IsSuccessStatusCode = false,
            StatusCode = statusCode,
            Content = (TournamentDetailResponse?)null
        };

        _mockApi
            .Setup(x => x.GetTournamentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }
}