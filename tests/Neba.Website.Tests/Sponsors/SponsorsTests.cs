using Bunit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Sponsors;
using Neba.Domain.Sponsors;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Sponsors;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Services;

using Refit;

using SponsorsPage = Neba.Website.Server.Sponsors.Sponsors;

namespace Neba.Website.Tests.Sponsors;

[UnitTest]
[Component("Website.Sponsors.Sponsors")]
public sealed class SponsorsTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly Mock<ISponsorsApi> _mockApi;

    public SponsorsTests()
    {
        _mockApi = new Mock<ISponsorsApi>(MockBehavior.Strict);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        _ctx.Services.AddSingleton(_mockApi.Object);
        _ctx.Services.AddSingleton(new ApiExecutor(mockStopwatch.Object, NullLogger<ApiExecutor>.Instance));
    }

    public void Dispose() => _ctx.Dispose();

    // ── Initialization ───────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render page title")]
    public void Render_ShouldShowPageTitle_WhenRendered()
    {
        SetupSuccessResponse([]);

        var cut = _ctx.Render<SponsorsPage>();

        cut.Markup.ShouldContain("Our Partners");
    }

    [Fact(DisplayName = "Should call ListActiveSponsorsAsync on initialization")]
    public void OnInit_ShouldCallListActiveSponsorsApi()
    {
        SetupSuccessResponse([]);

        _ctx.Render<SponsorsPage>();

        _mockApi.Verify(
            x => x.ListActiveSponsorsAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Loading skeleton ─────────────────────────────────────────────────────

    [Fact(DisplayName = "Should show loading skeleton while API is pending")]
    public void Render_ShouldShowLoadingSkeleton_WhileLoading()
    {
        _mockApi
            .Setup(x => x.ListActiveSponsorsAsync(It.IsAny<CancellationToken>()))
            .Returns(new TaskCompletionSource<IApiResponse<CollectionResponse<SponsorSummaryResponse>>>().Task);

        var cut = _ctx.Render<SponsorsPage>();

        cut.Markup.ShouldContain("aria-busy=\"true\"");
    }

    // ── Error state ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should show error alert when API call fails")]
    public void Render_ShouldShowErrorAlert_WhenApiFails()
    {
        SetupFailureResponse(System.Net.HttpStatusCode.InternalServerError);

        var cut = _ctx.Render<SponsorsPage>();

        cut.Markup.ShouldContain("Error Loading Sponsors");
    }

    // ── CTA section always visible ───────────────────────────────────────────

    [Fact(DisplayName = "Should show CTA section while loading")]
    public void Render_ShouldShowCtaSection_WhileLoading()
    {
        _mockApi
            .Setup(x => x.ListActiveSponsorsAsync(It.IsAny<CancellationToken>()))
            .Returns(new TaskCompletionSource<IApiResponse<CollectionResponse<SponsorSummaryResponse>>>().Task);

        var cut = _ctx.Render<SponsorsPage>();

        cut.Markup.ShouldContain("Want to join the legacy?");
    }

    [Fact(DisplayName = "Should show CTA section after API succeeds")]
    public void Render_ShouldShowCtaSection_WhenApiSucceeds()
    {
        SetupSuccessResponse([]);

        var cut = _ctx.Render<SponsorsPage>();

        cut.Markup.ShouldContain("Want to join the legacy?");
    }

    [Fact(DisplayName = "Should show CTA section when API fails")]
    public void Render_ShouldShowCtaSection_WhenApiFails()
    {
        SetupFailureResponse(System.Net.HttpStatusCode.InternalServerError);

        var cut = _ctx.Render<SponsorsPage>();

        cut.Markup.ShouldContain("Want to join the legacy?");
    }

    // ── Title sponsor ────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should show title sponsor section when title sponsor exists")]
    public void Render_ShouldShowTitleSponsorSection_WhenTitleSponsorExists()
    {
        var sponsor = SponsorSummaryResponseFactory.Create(name: "Acme Corp", tier: SponsorTier.TitleSponsor);
        SetupSuccessResponse([sponsor]);

        var cut = _ctx.Render<SponsorsPage>();

        cut.Markup.ShouldContain("Title Sponsor");
        cut.Markup.ShouldContain("Acme Corp");
    }

    [Fact(DisplayName = "Should not show title sponsor section when no title sponsor exists")]
    public void Render_ShouldNotShowTitleSponsorSection_WhenNoTitleSponsor()
    {
        SetupSuccessResponse([]);

        var cut = _ctx.Render<SponsorsPage>();

        cut.Markup.ShouldNotContain("Title Sponsor");
    }

    [Fact(DisplayName = "Should show title sponsor tag phrase when provided")]
    public void Render_ShouldShowTitleSponsorTagPhrase_WhenProvided()
    {
        var sponsor = SponsorSummaryResponseFactory.Create(
            tier: SponsorTier.TitleSponsor,
            tagPhrase: "Excellence in Motion");
        SetupSuccessResponse([sponsor]);

        var cut = _ctx.Render<SponsorsPage>();

        cut.Markup.ShouldContain("Excellence in Motion");
    }

    [Fact(DisplayName = "Should show title sponsor description when provided")]
    public void Render_ShouldShowTitleSponsorDescription_WhenProvided()
    {
        var sponsor = SponsorSummaryResponseFactory.Create(
            tier: SponsorTier.TitleSponsor,
            description: "Leaders in bowling equipment.");
        SetupSuccessResponse([sponsor]);

        var cut = _ctx.Render<SponsorsPage>();

        cut.Markup.ShouldContain("Leaders in bowling equipment.");
    }

    [Fact(DisplayName = "Should show Visit Website link for title sponsor when website URL provided")]
    public void Render_ShouldShowVisitWebsiteLink_WhenTitleSponsorHasWebsiteUrl()
    {
        var sponsor = SponsorSummaryResponseFactory.Create(
            tier: SponsorTier.TitleSponsor,
            websiteUrl: new Uri("https://acme.example.com"));
        SetupSuccessResponse([sponsor]);

        var cut = _ctx.Render<SponsorsPage>();

        cut.Markup.ShouldContain("Visit Website");
        cut.Markup.ShouldContain("https://acme.example.com");
    }

    [Fact(DisplayName = "Should not show Visit Website link for title sponsor when no website URL")]
    public void Render_ShouldNotShowVisitWebsiteLink_WhenTitleSponsorHasNoWebsiteUrl()
    {
        var sponsor = SponsorSummaryResponseFactory.Create(
            tier: SponsorTier.TitleSponsor,
            websiteUrl: null);
        SetupSuccessResponse([sponsor]);

        var cut = _ctx.Render<SponsorsPage>();

        cut.Markup.ShouldNotContain("Visit Website");
    }

    [Fact(DisplayName = "Should show Facebook link for title sponsor when Facebook URL provided")]
    public void Render_ShouldShowFacebookLink_WhenTitleSponsorHasFacebookUrl()
    {
        var sponsor = SponsorSummaryResponseFactory.Create(
            tier: SponsorTier.TitleSponsor,
            facebookUrl: new Uri("https://facebook.com/acme"));
        SetupSuccessResponse([sponsor]);

        var cut = _ctx.Render<SponsorsPage>();

        cut.Markup.ShouldContain("https://facebook.com/acme");
    }

    [Fact(DisplayName = "Should show Instagram link for title sponsor when Instagram URL provided")]
    public void Render_ShouldShowInstagramLink_WhenTitleSponsorHasInstagramUrl()
    {
        var sponsor = SponsorSummaryResponseFactory.Create(
            tier: SponsorTier.TitleSponsor,
            instagramUrl: new Uri("https://instagram.com/acme"));
        SetupSuccessResponse([sponsor]);

        var cut = _ctx.Render<SponsorsPage>();

        cut.Markup.ShouldContain("https://instagram.com/acme");
    }

    [Fact(DisplayName = "Should use fallback logo when title sponsor has no logo URL")]
    public void Render_ShouldUseFallbackLogo_WhenTitleSponsorHasNoLogoUrl()
    {
        var sponsor = SponsorSummaryResponseFactory.Create(
            tier: SponsorTier.TitleSponsor,
            logoUrl: null);
        SetupSuccessResponse([sponsor]);

        var cut = _ctx.Render<SponsorsPage>();

        cut.Markup.ShouldContain("/images/neba-logo.png");
    }

    [Fact(DisplayName = "Should use sponsor logo when title sponsor has logo URL")]
    public void Render_ShouldUseSponsorLogo_WhenTitleSponsorHasLogoUrl()
    {
        var sponsor = SponsorSummaryResponseFactory.Create(
            tier: SponsorTier.TitleSponsor,
            logoUrl: new Uri("https://cdn.example.com/acme-logo.png"));
        SetupSuccessResponse([sponsor]);

        var cut = _ctx.Render<SponsorsPage>();

        cut.Markup.ShouldContain("https://cdn.example.com/acme-logo.png");
        cut.Markup.ShouldNotContain("/images/neba-logo.png");
    }

    // ── Premier partners ─────────────────────────────────────────────────────

    [Fact(DisplayName = "Should show Premier Partners section when premium sponsors exist")]
    public void Render_ShouldShowPremierPartnersSection_WhenPremiumSponsorsExist()
    {
        var sponsor = SponsorSummaryResponseFactory.Create(name: "Premier Co", tier: SponsorTier.Premium);
        SetupSuccessResponse([sponsor]);

        var cut = _ctx.Render<SponsorsPage>();

        cut.Markup.ShouldContain("Premier Partners");
        cut.Markup.ShouldContain("Premier Co");
    }

    [Fact(DisplayName = "Should not show Premier Partners section when no premium sponsors exist")]
    public void Render_ShouldNotShowPremierPartnersSection_WhenNoPremiumSponsors()
    {
        SetupSuccessResponse([]);

        var cut = _ctx.Render<SponsorsPage>();

        cut.Markup.ShouldNotContain("Premier Partners");
    }

    [Fact(DisplayName = "Should show premier sponsor category")]
    public void Render_ShouldShowPremierSponsorCategory_WhenApiSucceeds()
    {
        var sponsor = SponsorSummaryResponseFactory.Create(
            tier: SponsorTier.Premium,
            category: SponsorCategory.Technology);
        SetupSuccessResponse([sponsor]);

        var cut = _ctx.Render<SponsorsPage>();

        cut.Markup.ShouldContain(SponsorCategory.Technology.Name);
    }

    [Fact(DisplayName = "Should link to sponsor details page for premier sponsor")]
    public void Render_ShouldLinkToSponsorDetails_WhenPremierSponsorRendered()
    {
        var sponsor = SponsorSummaryResponseFactory.Create(
            tier: SponsorTier.Premium,
            slug: "premier-sponsor",
            websiteUrl: new Uri("https://premier.example.com"));
        SetupSuccessResponse([sponsor]);

        var cut = _ctx.Render<SponsorsPage>();

        cut.Markup.ShouldContain("/sponsors/premier-sponsor");
        cut.Markup.ShouldContain("Learn more about");
    }

    [Fact(DisplayName = "Should still link to sponsor details page when premier sponsor has no website URL")]
    public void Render_ShouldLinkToSponsorDetails_WhenPremierSponsorHasNoWebsiteUrl()
    {
        var sponsor = SponsorSummaryResponseFactory.Create(
            tier: SponsorTier.Premium,
            slug: "premier-no-website",
            websiteUrl: null);
        SetupSuccessResponse([sponsor]);

        var cut = _ctx.Render<SponsorsPage>();

        cut.Markup.ShouldContain("/sponsors/premier-no-website");
    }

    [Fact(DisplayName = "Should render premier sponsors ordered by priority then name")]
    public void Render_ShouldOrderPremierSponsorsByPriorityThenName()
    {
        SetupSuccessResponse([
            SponsorSummaryResponseFactory.Create(name: "Zebra Co", priority: 1, tier: SponsorTier.Premium),
            SponsorSummaryResponseFactory.Create(name: "Alpha Co", priority: 2, tier: SponsorTier.Premium),
            SponsorSummaryResponseFactory.Create(name: "Middle Co", priority: 1, tier: SponsorTier.Premium),
        ]);

        var cut = _ctx.Render<SponsorsPage>();
        var markup = cut.Markup;

        // Priority 1 before priority 2
        markup.IndexOf("Zebra Co", StringComparison.Ordinal)
            .ShouldBeLessThan(markup.IndexOf("Alpha Co", StringComparison.Ordinal));
        // Within same priority, alphabetical: Middle before Zebra
        markup.IndexOf("Middle Co", StringComparison.Ordinal)
            .ShouldBeLessThan(markup.IndexOf("Zebra Co", StringComparison.Ordinal));
    }

    // ── Association sponsors ─────────────────────────────────────────────────

    [Fact(DisplayName = "Should show Association Sponsors section when standard sponsors exist")]
    public void Render_ShouldShowAssociationSponsorsSection_WhenStandardSponsorsExist()
    {
        var sponsor = SponsorSummaryResponseFactory.Create(name: "Standard Corp", tier: SponsorTier.Standard);
        SetupSuccessResponse([sponsor]);

        var cut = _ctx.Render<SponsorsPage>();

        cut.Markup.ShouldContain("Association Sponsors");
        cut.Markup.ShouldContain("Standard Corp");
    }

    [Fact(DisplayName = "Should not show Association Sponsors section when no standard sponsors exist")]
    public void Render_ShouldNotShowAssociationSponsorsSection_WhenNoStandardSponsors()
    {
        SetupSuccessResponse([]);

        var cut = _ctx.Render<SponsorsPage>();

        cut.Markup.ShouldNotContain("Association Sponsors");
    }

    [Fact(DisplayName = "Should link to sponsor details page when association sponsor has website URL")]
    public void Render_ShouldLinkToSponsorDetails_WhenAssociationSponsorHasWebsiteUrl()
    {
        var sponsor = SponsorSummaryResponseFactory.Create(
            tier: SponsorTier.Standard,
            slug: "association-sponsor",
            websiteUrl: new Uri("https://assoc.example.com"));
        SetupSuccessResponse([sponsor]);

        var cut = _ctx.Render<SponsorsPage>();

        cut.Markup.ShouldContain("/sponsors/association-sponsor");
        cut.Markup.ShouldNotContain("https://assoc.example.com");
    }

    [Fact(DisplayName = "Should link to sponsor details page when association sponsor has no website URL")]
    public void Render_ShouldLinkToSponsorDetails_WhenAssociationSponsorHasNoWebsiteUrl()
    {
        var sponsor = SponsorSummaryResponseFactory.Create(
            tier: SponsorTier.Standard,
            slug: "some-sponsor",
            websiteUrl: null);
        SetupSuccessResponse([sponsor]);

        var cut = _ctx.Render<SponsorsPage>();

        cut.Find(".sponsor-tile").GetAttribute("href").ShouldBe("/sponsors/some-sponsor");
    }

    [Fact(DisplayName = "Should render association sponsors ordered by priority then name")]
    public void Render_ShouldOrderAssociationSponsorsByPriorityThenName()
    {
        SetupSuccessResponse([
            SponsorSummaryResponseFactory.Create(name: "Zebra Assoc", priority: 1, tier: SponsorTier.Standard),
            SponsorSummaryResponseFactory.Create(name: "Alpha Assoc", priority: 2, tier: SponsorTier.Standard),
            SponsorSummaryResponseFactory.Create(name: "Middle Assoc", priority: 1, tier: SponsorTier.Standard),
        ]);

        var cut = _ctx.Render<SponsorsPage>();
        var markup = cut.Markup;

        // Priority 1 before priority 2
        markup.IndexOf("Zebra Assoc", StringComparison.Ordinal)
            .ShouldBeLessThan(markup.IndexOf("Alpha Assoc", StringComparison.Ordinal));
        // Within same priority, alphabetical: Middle before Zebra
        markup.IndexOf("Middle Assoc", StringComparison.Ordinal)
            .ShouldBeLessThan(markup.IndexOf("Zebra Assoc", StringComparison.Ordinal));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void SetupSuccessResponse(IReadOnlyCollection<SponsorSummaryResponse> sponsors)
    {
        var response = new Mock<IApiResponse<CollectionResponse<SponsorSummaryResponse>>>(MockBehavior.Strict);
        response.Setup(r => r.IsSuccessStatusCode).Returns(true);
        response.Setup(r => r.StatusCode).Returns(System.Net.HttpStatusCode.OK);
        response.Setup(r => r.Content).Returns(new CollectionResponse<SponsorSummaryResponse> { Items = sponsors });

        _mockApi
            .Setup(x => x.ListActiveSponsorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response.Object);
    }

    private void SetupFailureResponse(System.Net.HttpStatusCode statusCode)
    {
        var response = new Mock<IApiResponse<CollectionResponse<SponsorSummaryResponse>>>(MockBehavior.Strict);
        response.Setup(r => r.IsSuccessStatusCode).Returns(false);
        response.Setup(r => r.StatusCode).Returns(statusCode);
        response.Setup(r => r.Content).Returns((CollectionResponse<SponsorSummaryResponse>?)null);

        _mockApi
            .Setup(x => x.ListActiveSponsorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response.Object);
    }
}