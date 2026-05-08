using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.Tournaments;
using Neba.Api.Contracts.Tournaments.GetTournament;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Services;
using Neba.Website.Server.Tournaments.Detail;

using Refit;

namespace Neba.Website.Tests.Tournaments.Detail;

[UnitTest]
[Component("Website.Tournaments.Detail.TournamentDetail")]
public sealed class TournamentDetailTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly Mock<ITournamentsApi> _mockApi;

    public TournamentDetailTests()
    {
        _mockApi = new Mock<ITournamentsApi>(MockBehavior.Strict);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        _ctx.Services.AddSingleton(_mockApi.Object);
        _ctx.Services.AddSingleton(new ApiExecutor(mockStopwatch.Object, NullLogger<ApiExecutor>.Instance));
    }

    public void Dispose() => _ctx.Dispose();

    // ── Loading state ────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should show loading spinner while API is pending")]
    public void Render_ShouldShowLoadingSpinner_WhileLoading()
    {
        _mockApi
            .Setup(x => x.GetTournamentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(new TaskCompletionSource<IApiResponse<TournamentDetailResponse>>().Task);

        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        cut.Markup.ShouldContain("neba-spinner");
    }

    // ── Initialization ───────────────────────────────────────────────────────

    [Fact(DisplayName = "Should call GetTournamentAsync with the Id parameter")]
    public void OnInit_ShouldCallApiWithId()
    {
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(id: TournamentDetailResponseFactory.ValidId));

        _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        _mockApi.Verify(
            x => x.GetTournamentAsync(TournamentDetailResponseFactory.ValidId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Navigation on error ──────────────────────────────────────────────────

    [Fact(DisplayName = "Should navigate to /not-found when API call fails")]
    public void OnInit_ShouldNavigateToNotFound_WhenApiFails()
    {
        SetupFailureResponse(System.Net.HttpStatusCode.NotFound);

        _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        nav.Uri.ShouldEndWith("/not-found");
    }

    // ── Page title ───────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render page title with tournament name")]
    public void Render_ShouldRenderPageTitle_WithTournamentName()
    {
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(name: "Spring Open"));

        _ = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));
        var headOutlet = _ctx.Render<HeadOutlet>();

        headOutlet.Find("title").TextContent.ShouldBe("Spring Open | NEBA Tournaments");
    }

    // ── Header ───────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render tournament name in h1")]
    public void Render_ShouldRenderTournamentName_InHeading()
    {
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(name: "Winter Classic"));

        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        cut.Find("h1").TextContent.ShouldContain("Winter Classic");
    }

    [Fact(DisplayName = "Should render tournament type chip")]
    public void Render_ShouldRenderTournamentTypeChip()
    {
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            tournamentType: Neba.Domain.Tournaments.TournamentType.Doubles));

        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        cut.Markup.ShouldContain("Doubles");
    }

    [Fact(DisplayName = "Should render bowling center location when host is set")]
    public void Render_ShouldRenderLocation_WhenHostSet()
    {
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            bowlingCenter: TournamentDetailBowlingCenterResponseFactory.Create(
                name: "Striker Lanes", city: "Manchester", state: "NH")));

        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        cut.Markup.ShouldContain("Striker Lanes");
        cut.Markup.ShouldContain("Manchester");
    }

    [Fact(DisplayName = "Should not render location when no bowling center is assigned")]
    public void Render_ShouldNotRenderLocation_WhenNoBowlingCenter()
    {
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(bowlingCenter: null));

        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        cut.Find(".td-hero__eyebrow").TextContent.ShouldNotContain("Lanes");
    }

    [Fact(DisplayName = "Should render pattern length category chip when set")]
    public void Render_ShouldRenderPatternLengthChip_WhenSet()
    {
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(patternLengthCategory: "Medium"));

        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        cut.Markup.ShouldContain("Medium");
    }

    [Fact(DisplayName = "Should not render pattern length chip when null")]
    public void Render_ShouldNotRenderPatternLengthChip_WhenNull()
    {
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(patternLengthCategory: null));

        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        cut.FindAll(".td-hero__chips .td-hero__chip").Count.ShouldBe(1);
    }

    // ── Champion bar ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render champion bar when winners are present")]
    public void Render_ShouldRenderChampionBar_WhenWinnersPresent()
    {
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            winners: ["Alex Example", "Jamie Sample"]));

        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        cut.Find(".tournament-detail__champion-bar").TextContent.ShouldContain("Alex Example / Jamie Sample");
    }

    [Fact(DisplayName = "Should not render champion bar when no winners")]
    public void Render_ShouldNotRenderChampionBar_WhenNoWinners()
    {
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(winners: []));

        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        cut.FindAll(".tournament-detail__champion-bar").ShouldBeEmpty();
    }

    // ── Info card (upcoming) ─────────────────────────────────────────────────

    [Fact(DisplayName = "Should render info card with price and register link for upcoming tournament")]
    public void Render_ShouldRenderInfoCard_ForUpcomingTournament()
    {
        var futureDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30));
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            startDate: futureDate,
            endDate: futureDate,
            entryFee: 95m,
            addedMoney: 1500m,
            registrationUrl: new Uri("https://bowlneba.com/register")));

        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        cut.FindAll(".td-rail-card").Count.ShouldBe(1);
        cut.Markup.ShouldContain("$95");
        cut.Markup.ShouldContain("$1,500");
        cut.Markup.ShouldContain("Register");
    }

    [Fact(DisplayName = "Should not render info card for past tournament")]
    public void Render_ShouldNotRenderInfoCard_ForPastTournament()
    {
        var pastDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            startDate: pastDate,
            endDate: pastDate));

        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        cut.FindAll(".td-rail-card").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should not render register button when registration URL is absent")]
    public void Render_ShouldNotRenderRegisterButton_WhenNoRegistrationUrl()
    {
        var futureDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30));
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            startDate: futureDate,
            endDate: futureDate,
            registrationUrl: null));

        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        cut.Markup.ShouldNotContain("Register");
    }

    // ── Entry count ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render entry count when present")]
    public void Render_ShouldRenderEntryCount_WhenPresent()
    {
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(entryCount: 64));

        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        cut.Find(".td-rail-entries__value").TextContent.ShouldContain("64");
    }

    [Fact(DisplayName = "Should not render entry count when null")]
    public void Render_ShouldNotRenderEntryCount_WhenNull()
    {
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(entryCount: null));

        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        cut.FindAll(".td-rail-entries").ShouldBeEmpty();
    }

    // ── Oil patterns ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render oil patterns section when patterns are present")]
    public void Render_ShouldRenderOilPatterns_WhenPresent()
    {
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            oilPatterns: [TournamentDetailOilPatternResponseFactory.Create(
                name: "Kegel Broadway", length: 40, rounds: ["Qualifying", "Finals"])]));

        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        cut.Markup.ShouldContain("Oil Patterns");
        cut.Markup.ShouldContain("Kegel Broadway · 40 ft");
        cut.Markup.ShouldContain("Qualifying, Finals");
    }

    [Fact(DisplayName = "Should not render oil patterns section when no patterns")]
    public void Render_ShouldNotRenderOilPatterns_WhenEmpty()
    {
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(oilPatterns: []));

        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        cut.Markup.ShouldNotContain("Oil Patterns");
    }

    // ── Sponsors ─────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render sponsors section linking to sponsor detail pages")]
    public void Render_ShouldRenderSponsors_WithDetailLinks()
    {
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            sponsors: [TournamentDetailSponsorResponseFactory.Create(
                name: "Acme Corp", slug: "acme-corp")]));

        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        cut.Markup.ShouldContain("Sponsors");
        cut.Markup.ShouldContain("/sponsors/acme-corp");
    }

    [Fact(DisplayName = "Should render sponsor logo image when logo URL is present")]
    public void Render_ShouldRenderSponsorLogo_WhenLogoUrlPresent()
    {
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            sponsors: [TournamentDetailSponsorResponseFactory.Create(
                logoUrl: new Uri("https://cdn.example.com/acme-logo.png"))]));

        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        cut.Markup.ShouldContain("https://cdn.example.com/acme-logo.png");
        cut.FindAll(".td-rail-sponsor-card__name").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render sponsor name text when no logo URL")]
    public void Render_ShouldRenderSponsorName_WhenNoLogoUrl()
    {
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            sponsors: [TournamentDetailSponsorResponseFactory.Create(
                name: "Acme Corp", logoUrl: null)]));

        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        cut.Find(".td-rail-sponsor-card__name").TextContent.ShouldBe("Acme Corp");
    }

    [Fact(DisplayName = "Should not render sponsors section when no sponsors")]
    public void Render_ShouldNotRenderSponsors_WhenEmpty()
    {
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(sponsors: []));

        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        cut.FindAll(".td-rail-sponsors").ShouldBeEmpty();
    }

    // ── Results ──────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render results section with main cut bowler name")]
    public void Render_ShouldRenderResults_WithMainCutBowlerName()
    {
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            results: [TournamentResultResponseFactory.Create(
                bowlerName: "Jane Smith", place: 1, sideCutName: null)]));

        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        cut.Markup.ShouldContain("Results");
        cut.Markup.ShouldContain("Jane Smith");
    }

    [Fact(DisplayName = "Should render side cut sections grouped by name")]
    public void Render_ShouldRenderSideCutSections_GroupedByName()
    {
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            results:
            [
                TournamentResultResponseFactory.Create(bowlerName: "Jane Smith", sideCutName: "Senior"),
                TournamentResultResponseFactory.Create(bowlerName: "Bob Jones", sideCutName: "Senior"),
            ]));

        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        cut.FindAll(".tournament-detail__cut-section").Count.ShouldBe(1);
        cut.Find(".tournament-detail__cut-title").TextContent.Trim().ShouldBe("Senior");
        cut.Markup.ShouldContain("Jane Smith");
        cut.Markup.ShouldContain("Bob Jones");
    }

    [Fact(DisplayName = "Should show no-results message for past tournament without results")]
    public void Render_ShouldShowNoResultsMessage_WhenPastTournamentHasNoResults()
    {
        var pastDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            startDate: pastDate,
            endDate: pastDate,
            results: []));

        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        cut.Find(".tournament-detail__no-results").TextContent.ShouldContain("Results are not yet available");
    }

    [Fact(DisplayName = "Should not show no-results message for upcoming tournament without results")]
    public void Render_ShouldNotShowNoResultsMessage_WhenUpcomingTournamentHasNoResults()
    {
        var futureDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30));
        SetupSuccessResponse(TournamentDetailResponseFactory.Create(
            startDate: futureDate,
            endDate: futureDate,
            results: []));

        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        cut.FindAll(".tournament-detail__no-results").ShouldBeEmpty();
    }

    // ── Footer ───────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render back link to tournament schedule")]
    public void Render_ShouldRenderBackLink_ToTournamentSchedule()
    {
        SetupSuccessResponse(TournamentDetailResponseFactory.Create());

        var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

        cut.Find(".tournament-detail__back-link").GetAttribute("href").ShouldBe("/tournaments");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void SetupSuccessResponse(TournamentDetailResponse tournament)
    {
        var response = new Mock<IApiResponse<TournamentDetailResponse>>(MockBehavior.Strict);
        response.Setup(r => r.IsSuccessStatusCode).Returns(true);
        response.Setup(r => r.StatusCode).Returns(System.Net.HttpStatusCode.OK);
        response.Setup(r => r.Content).Returns(tournament);

        _mockApi
            .Setup(x => x.GetTournamentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response.Object);
    }

    private void SetupFailureResponse(System.Net.HttpStatusCode statusCode)
    {
        var response = new Mock<IApiResponse<TournamentDetailResponse>>(MockBehavior.Strict);
        response.Setup(r => r.IsSuccessStatusCode).Returns(false);
        response.Setup(r => r.StatusCode).Returns(statusCode);
        response.Setup(r => r.Content).Returns((TournamentDetailResponse?)null);

        _mockApi
            .Setup(x => x.GetTournamentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response.Object);
    }
}