using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.Sponsors;
using Neba.Domain.Sponsors;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Contact;
using Neba.TestFactory.Sponsors;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Services;
using Neba.Website.Server.Sponsors;

using Refit;

namespace Neba.Website.Tests.Sponsors;

[UnitTest]
[Component("Website.Sponsors.SponsorDetail")]
public sealed class SponsorDetailTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly Mock<ISponsorsApi> _mockApi;

    public SponsorDetailTests()
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

    // ── Loading state ────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should show loading spinner while API is pending")]
    public void Render_ShouldShowLoadingSpinner_WhileLoading()
    {
        _mockApi
            .Setup(x => x.GetSponsorBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(new TaskCompletionSource<IApiResponse<SponsorDetailResponse>>().Task);

        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        cut.Markup.ShouldContain("neba-spinner");
    }

    // ── Initialization ───────────────────────────────────────────────────────

    [Fact(DisplayName = "Should call GetSponsorBySlugAsync with the Slug parameter")]
    public void OnInit_ShouldCallApiWithSlug()
    {
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(slug: "acme-corp"));

        _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "acme-corp"));

        _mockApi.Verify(
            x => x.GetSponsorBySlugAsync("acme-corp", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Navigation on error/inactive ─────────────────────────────────────────

    [Fact(DisplayName = "Should navigate to /not-found when API call fails")]
    public void OnInit_ShouldNavigateToNotFound_WhenApiFails()
    {
        SetupFailureResponse(System.Net.HttpStatusCode.NotFound);

        _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "unknown-slug"));

        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        nav.Uri.ShouldEndWith("/not-found");
    }

    [Fact(DisplayName = "Should navigate to /not-found when sponsor is inactive")]
    public void OnInit_ShouldNavigateToNotFound_WhenSponsorIsInactive()
    {
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(isCurrentSponsor: false));

        _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "inactive-sponsor"));

        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        nav.Uri.ShouldEndWith("/not-found");
    }

    // ── Page title ───────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render page title with sponsor name")]
    public void Render_ShouldRenderPageTitle_WithSponsorName()
    {
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(name: "Acme Bowling Supply"));

        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "acme-bowling-supply"));

        cut.Markup.ShouldContain("Acme Bowling Supply | NEBA Sponsors");
    }

    // ── Header ───────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render tier badge")]
    public void Render_ShouldRenderTierBadge()
    {
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(tier: SponsorTier.Premier));

        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "premier-co"));

        cut.Markup.ShouldContain(SponsorTier.Premier.Name);
    }

    [Fact(DisplayName = "Should render sponsor name in h1")]
    public void Render_ShouldRenderSponsorName_InHeading()
    {
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(name: "Premier Lanes"));

        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "premier-lanes"));

        cut.Find("h1").TextContent.ShouldContain("Premier Lanes");
    }

    [Fact(DisplayName = "Should render tagline when provided")]
    public void Render_ShouldRenderTagline_WhenProvided()
    {
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(tagPhrase: "Strike Up Excellence"));

        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        cut.Markup.ShouldContain("Strike Up Excellence");
    }

    [Fact(DisplayName = "Should not render tagline element when not provided")]
    public void Render_ShouldNotRenderTagline_WhenNull()
    {
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(tagPhrase: null));

        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        cut.FindAll(".sponsor-detail__tagline").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render logo image when LogoUrl is provided")]
    public void Render_ShouldRenderLogoImage_WhenLogoUrlProvided()
    {
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(
            logoUrl: new Uri("https://cdn.example.com/acme-logo.png")));

        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        cut.Markup.ShouldContain("https://cdn.example.com/acme-logo.png");
        cut.FindAll(".sponsor-detail__logo-placeholder").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render logo placeholder when LogoUrl is null")]
    public void Render_ShouldRenderLogoPlaceholder_WhenNoLogoUrl()
    {
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(logoUrl: null));

        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        cut.FindAll(".sponsor-detail__logo-placeholder").Count.ShouldBe(1);
        cut.FindAll(".sponsor-detail__logo-img").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render Visit Website link when WebsiteUrl is provided")]
    public void Render_ShouldRenderWebsiteLink_WhenProvided()
    {
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(
            websiteUrl: new Uri("https://acme.example.com")));

        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        cut.Markup.ShouldContain("Visit Website");
        cut.Markup.ShouldContain("https://acme.example.com");
    }

    [Fact(DisplayName = "Should not render Visit Website link when WebsiteUrl is null")]
    public void Render_ShouldNotRenderWebsiteLink_WhenNull()
    {
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(websiteUrl: null));

        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        cut.Markup.ShouldNotContain("Visit Website");
    }

    // ── About section ────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render about text when description is provided")]
    public void Render_ShouldRenderAboutText_WhenProvided()
    {
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(
            description: "Leaders in bowling equipment since 1982."));

        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        cut.Markup.ShouldContain("Leaders in bowling equipment since 1982.");
    }

    [Fact(DisplayName = "Should not render about section when description is null")]
    public void Render_ShouldNotRenderAboutSection_WhenDescriptionNull()
    {
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(description: null));

        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        cut.Markup.ShouldNotContain("About our Partner");
    }

    // ── Promotional info ─────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render promotional notes when provided")]
    public void Render_ShouldRenderPromotionalNotes_WhenProvided()
    {
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(
            promotionalNotes: "Exclusive 10% discount for NEBA members."));

        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        cut.Markup.ShouldContain("Exclusive 10% discount for NEBA members.");
    }

    [Fact(DisplayName = "Should render live read script when provided")]
    public void Render_ShouldRenderLiveReadScript_WhenProvided()
    {
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(
            liveReadText: "And now a word from our sponsor, Acme Bowling..."));

        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        cut.Markup.ShouldContain("And now a word from our sponsor, Acme Bowling...");
    }

    [Fact(DisplayName = "Should not render promotional section when no promotional data")]
    public void Render_ShouldNotRenderPromoSection_WhenNoPromoData()
    {
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(
            promotionalNotes: null,
            liveReadText: null));

        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        cut.Markup.ShouldNotContain("Member Exclusive");
    }

    // ── Contact card ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render business address when address fields are provided")]
    public void Render_ShouldRenderAddress_WhenProvided()
    {
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(
            businessStreet: "100 Bowl Drive",
            businessCity: "Springfield"));

        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        cut.Markup.ShouldContain("100 Bowl Drive");
        cut.Markup.ShouldContain("Springfield");
    }

    [Fact(DisplayName = "Should not render address section when all address fields are null")]
    public void Render_ShouldNotRenderAddress_WhenNoAddressFields()
    {
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(
            businessStreet: null,
            businessCity: null,
            businessState: null,
            businessPostalCode: null,
            businessCountry: null));

        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        cut.Markup.ShouldNotContain("Business Location");
    }

    [Fact(DisplayName = "Should render contact email link when provided")]
    public void Render_ShouldRenderContactEmail_WhenProvided()
    {
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(
            businessEmailAddress: "hello@acme.example.com"));

        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        cut.Markup.ShouldContain("mailto:hello@acme.example.com");
    }

    [Fact(DisplayName = "Should render phone numbers formatted as (XXX) XXX-XXXX")]
    public void Render_ShouldRenderPhoneNumbers_Formatted()
    {
        var phones = new[] { PhoneNumberResponseFactory.Create(number: "8005551234") };
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(phoneNumbers: phones));

        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        cut.Markup.ShouldContain("(800) 555-1234");
    }

    [Fact(DisplayName = "Should not render contact section when no email or phone numbers")]
    public void Render_ShouldNotRenderContactSection_WhenNoChannels()
    {
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(
            businessEmailAddress: null,
            phoneNumbers: []));

        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        cut.Markup.ShouldNotContain("Contact Channels");
    }

    // ── Social media ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render Facebook link when provided")]
    public void Render_ShouldRenderFacebookLink_WhenProvided()
    {
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(
            facebookUrl: new Uri("https://facebook.com/acme")));

        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        cut.Markup.ShouldContain("https://facebook.com/acme");
        cut.Markup.ShouldContain("aria-label=\"Facebook\"");
    }

    [Fact(DisplayName = "Should render Instagram link when provided")]
    public void Render_ShouldRenderInstagramLink_WhenProvided()
    {
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(
            instagramUrl: new Uri("https://instagram.com/acme")));

        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        cut.Markup.ShouldContain("https://instagram.com/acme");
        cut.Markup.ShouldContain("aria-label=\"Instagram\"");
    }

    [Fact(DisplayName = "Should not render social media section when no social URLs")]
    public void Render_ShouldNotRenderSocialSection_WhenNoSocialUrls()
    {
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(
            facebookUrl: null,
            instagramUrl: null));

        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        cut.Markup.ShouldNotContain("Social Media");
    }

    // ── Internal contact ─────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render internal contact section when contact name is provided")]
    public void Render_ShouldRenderInternalContact_WhenProvided()
    {
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(
            contactName: "Jane Smith",
            contactEmail: "jane@acme.internal"));

        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        cut.Markup.ShouldContain("Internal Sponsor Contact");
        cut.Markup.ShouldContain("Jane Smith");
    }

    [Fact(DisplayName = "Should not render internal contact section when no contact name")]
    public void Render_ShouldNotRenderInternalContact_WhenNoContactName()
    {
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(contactName: null));

        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        cut.Markup.ShouldNotContain("Internal Sponsor Contact");
    }

    // ── Footer ───────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render back link to sponsor directory")]
    public void Render_ShouldRenderBackLink_ToSponsorDirectory()
    {
        SetupSuccessResponse(SponsorDetailResponseFactory.Create());

        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        cut.Markup.ShouldContain("Back to Sponsor Directory");
        cut.Find(".sponsor-detail__back-link").GetAttribute("href").ShouldBe("/sponsors");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void SetupSuccessResponse(SponsorDetailResponse sponsor)
    {
        var response = new Mock<IApiResponse<SponsorDetailResponse>>(MockBehavior.Strict);
        response.Setup(r => r.IsSuccessStatusCode).Returns(true);
        response.Setup(r => r.StatusCode).Returns(System.Net.HttpStatusCode.OK);
        response.Setup(r => r.Content).Returns(sponsor);

        _mockApi
            .Setup(x => x.GetSponsorBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response.Object);
    }

    private void SetupFailureResponse(System.Net.HttpStatusCode statusCode)
    {
        var response = new Mock<IApiResponse<SponsorDetailResponse>>(MockBehavior.Strict);
        response.Setup(r => r.IsSuccessStatusCode).Returns(false);
        response.Setup(r => r.StatusCode).Returns(statusCode);
        response.Setup(r => r.Content).Returns((SponsorDetailResponse?)null);

        _mockApi
            .Setup(x => x.GetSponsorBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response.Object);
    }
}