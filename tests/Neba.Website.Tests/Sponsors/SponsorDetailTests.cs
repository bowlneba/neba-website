using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.Sponsors;
using Neba.Api.Features.Sponsors.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Contact;
using Neba.TestFactory.Sponsors;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Services;
using Neba.Website.Server.Sponsors;

using Refit;
using Refit.Testing;

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
        // Arrange
        _mockApi
            .Setup(x => x.GetSponsorBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(new TaskCompletionSource<IApiResponse<SponsorDetailResponse>>().Task);

        // Act
        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        // Assert
        cut.Markup.ShouldContain("neba-spinner");
    }

    // ── Initialization ───────────────────────────────────────────────────────

    [Fact(DisplayName = "Should call GetSponsorBySlugAsync with the Slug parameter")]
    public void OnInit_ShouldCallApiWithSlug()
    {
        // Arrange
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(slug: "acme-corp"));

        // Act
        _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "acme-corp"));

        // Assert
        _mockApi.Verify(
            x => x.GetSponsorBySlugAsync("acme-corp", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Navigation on error/inactive ─────────────────────────────────────────

    [Fact(DisplayName = "Should navigate to /not-found when API call fails")]
    public void OnInit_ShouldNavigateToNotFound_WhenApiFails()
    {
        // Arrange
        SetupFailureResponse(System.Net.HttpStatusCode.NotFound);

        // Act
        _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "unknown-slug"));

        // Assert
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        nav.Uri.ShouldEndWith("/not-found");
    }

    [Fact(DisplayName = "Should navigate to /not-found when sponsor is inactive")]
    public void OnInit_ShouldNavigateToNotFound_WhenSponsorIsInactive()
    {
        // Arrange
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(isCurrentSponsor: false));

        // Act
        _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "inactive-sponsor"));

        // Assert
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        nav.Uri.ShouldEndWith("/not-found");
    }

    // ── Page title ───────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render page title with sponsor name")]
    public void Render_ShouldRenderPageTitle_WithSponsorName()
    {
        // Arrange
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(name: "Acme Bowling Supply"));

        // Act
        _ = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "acme-bowling-supply"));
        var headOutlet = _ctx.Render<HeadOutlet>();

        // Assert
        headOutlet.Find("title").TextContent.ShouldBe("Acme Bowling Supply - BowlNEBA");
    }

    // ── Header ───────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render tier badge")]
    public void Render_ShouldRenderTierBadge()
    {
        // Arrange
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(tier: SponsorTier.Premier));

        // Act
        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "premier-co"));

        // Assert
        cut.Markup.ShouldContain(SponsorTier.Premier.Name);
    }

    [Fact(DisplayName = "Should render sponsor name in h1")]
    public void Render_ShouldRenderSponsorName_InHeading()
    {
        // Arrange
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(name: "Premier Lanes"));

        // Act
        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "premier-lanes"));

        // Assert
        cut.Find("h1").TextContent.ShouldContain("Premier Lanes");
    }

    [Fact(DisplayName = "Should render tagline when provided")]
    public void Render_ShouldRenderTagline_WhenProvided()
    {
        // Arrange
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(tagPhrase: "Strike Up Excellence"));

        // Act
        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        // Assert
        cut.Markup.ShouldContain("Strike Up Excellence");
    }

    [Fact(DisplayName = "Should not render tagline element when not provided")]
    public void Render_ShouldNotRenderTagline_WhenNull()
    {
        // Arrange
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(tagPhrase: null));

        // Act
        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        // Assert
        cut.FindAll(".sponsor-detail__tagline").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render logo image when LogoUrl is provided")]
    public void Render_ShouldRenderLogoImage_WhenLogoUrlProvided()
    {
        // Arrange
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(
            logoUrl: new Uri("https://cdn.example.com/acme-logo.png")));

        // Act
        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        // Assert
        cut.Markup.ShouldContain("https://cdn.example.com/acme-logo.png");
        cut.FindAll(".sponsor-detail__logo-placeholder").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render logo placeholder when LogoUrl is null")]
    public void Render_ShouldRenderLogoPlaceholder_WhenNoLogoUrl()
    {
        // Arrange
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(logoUrl: null));

        // Act
        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        // Assert
        cut.FindAll(".sponsor-detail__logo-placeholder").Count.ShouldBe(1);
        cut.FindAll(".sponsor-detail__logo-img").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render Visit Website link when WebsiteUrl is provided")]
    public void Render_ShouldRenderWebsiteLink_WhenProvided()
    {
        // Arrange
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(
            websiteUrl: new Uri("https://acme.example.com")));

        // Act
        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        // Assert
        cut.Markup.ShouldContain("Visit Website");
        cut.Markup.ShouldContain("https://acme.example.com");
    }

    [Fact(DisplayName = "Should not render Visit Website link when WebsiteUrl is null")]
    public void Render_ShouldNotRenderWebsiteLink_WhenNull()
    {
        // Arrange
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(websiteUrl: null));

        // Act
        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        // Assert
        cut.Markup.ShouldNotContain("Visit Website");
    }

    // ── About section ────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render about text when description is provided")]
    public void Render_ShouldRenderAboutText_WhenProvided()
    {
        // Arrange
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(
            description: "Leaders in bowling equipment since 1982."));

        // Act
        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        // Assert
        cut.Markup.ShouldContain("Leaders in bowling equipment since 1982.");
    }

    [Fact(DisplayName = "Should not render about section when description is null")]
    public void Render_ShouldNotRenderAboutSection_WhenDescriptionNull()
    {
        // Arrange
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(description: null));

        // Act
        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        // Assert
        cut.Markup.ShouldNotContain("About our Partner");
    }

    // ── Contact card ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render business address when address fields are provided")]
    public void Render_ShouldRenderAddress_WhenProvided()
    {
        // Arrange
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(
            businessStreet: "100 Bowl Drive",
            businessCity: "Springfield"));

        // Act
        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        // Assert
        cut.Markup.ShouldContain("100 Bowl Drive");
        cut.Markup.ShouldContain("Springfield");
    }

    [Fact(DisplayName = "Should not render address section when all address fields are null")]
    public void Render_ShouldNotRenderAddress_WhenNoAddressFields()
    {
        // Arrange
        SetupSuccessResponse(SponsorDetailResponseFactory.Create() with
        {
            BusinessStreet = null,
            BusinessCity = null,
            BusinessState = null,
            BusinessPostalCode = null,
            BusinessCountry = null
        });

        // Act
        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        // Assert
        cut.Markup.ShouldNotContain("Business Location");
    }

    [Fact(DisplayName = "Should render contact email link when provided")]
    public void Render_ShouldRenderContactEmail_WhenProvided()
    {
        // Arrange
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(
            businessEmailAddress: "hello@acme.example.com"));

        // Act
        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        // Assert
        cut.Markup.ShouldContain("mailto:hello@acme.example.com");
    }

    [Fact(DisplayName = "Should render phone numbers formatted as (XXX) XXX-XXXX")]
    public void Render_ShouldRenderPhoneNumbers_Formatted()
    {
        // Arrange
        var phones = new[] { PhoneNumberResponseFactory.Create(number: "8005551234") };
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(phoneNumbers: phones));

        // Act
        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        // Assert
        cut.Markup.ShouldContain("(800) 555-1234");
    }

    [Fact(DisplayName = "Should not render contact section when no email or phone numbers")]
    public void Render_ShouldNotRenderContactSection_WhenNoChannels()
    {
        // Arrange
        SetupSuccessResponse(SponsorDetailResponseFactory.Create() with
        {
            BusinessEmailAddress = null,
            PhoneNumbers = []
        });

        // Act
        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        // Assert
        cut.Markup.ShouldNotContain("Contact Channels");
    }

    // ── Social media ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render Facebook link when provided")]
    public void Render_ShouldRenderFacebookLink_WhenProvided()
    {
        // Arrange
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(
            facebookUrl: new Uri("https://facebook.com/acme")));

        // Act
        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        // Assert
        cut.Markup.ShouldContain("https://facebook.com/acme");
        cut.Markup.ShouldContain("aria-label=\"Facebook\"");
    }

    [Fact(DisplayName = "Should render Instagram link when provided")]
    public void Render_ShouldRenderInstagramLink_WhenProvided()
    {
        // Arrange
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(
            instagramUrl: new Uri("https://instagram.com/acme")));

        // Act
        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        // Assert
        cut.Markup.ShouldContain("https://instagram.com/acme");
        cut.Markup.ShouldContain("aria-label=\"Instagram\"");
    }

    [Fact(DisplayName = "Should not render social media section when no social URLs")]
    public void Render_ShouldNotRenderSocialSection_WhenNoSocialUrls()
    {
        // Arrange
        SetupSuccessResponse(SponsorDetailResponseFactory.Create(
            facebookUrl: null,
            instagramUrl: null));

        // Act
        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        // Assert
        cut.Markup.ShouldNotContain("Social Media");
    }

    // ── Footer ───────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render back link to sponsor directory")]
    public void Render_ShouldRenderBackLink_ToSponsorDirectory()
    {
        // Arrange
        SetupSuccessResponse(SponsorDetailResponseFactory.Create());

        // Act
        var cut = _ctx.Render<SponsorDetail>(p => p.Add(x => x.Slug, "test-slug"));

        // Assert
        cut.Markup.ShouldContain("Back to Sponsor Directory");
        cut.Find(".sponsor-detail__back-link").GetAttribute("href").ShouldBe("/sponsors");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void SetupSuccessResponse(SponsorDetailResponse sponsor)
    {
        using var response = new StubApiResponse<SponsorDetailResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = sponsor
        };

        _mockApi
            .Setup(x => x.GetSponsorBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupFailureResponse(System.Net.HttpStatusCode statusCode)
    {
        using var response = new StubApiResponse<SponsorDetailResponse>
        {
            IsSuccessStatusCode = false,
            StatusCode = statusCode,
            Content = (SponsorDetailResponse?)null
        };

        _mockApi
            .Setup(x => x.GetSponsorBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }
}