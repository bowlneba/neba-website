using Bunit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Sponsors;
using Neba.Api.Features.Sponsors.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Sponsors;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Services;

using Refit;

using AboutPage = Neba.Website.Server.About.About;

namespace Neba.Website.Tests.About;

[UnitTest]
[Component("Website.About.About")]
public sealed class AboutPageTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly Mock<ISponsorsApi> _mockApi;

    public AboutPageTests()
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

    [Fact(DisplayName = "Should show loading skeleton while API is pending")]
    public void Render_ShouldShowLoadingSkeleton_WhileLoading()
    {
        // Arrange
        _mockApi
            .Setup(x => x.ListActiveSponsorsAsync(It.IsAny<CancellationToken>()))
            .Returns(new TaskCompletionSource<IApiResponse<CollectionResponse<SponsorSummaryResponse>>>().Task);

        // Act
        var cut = _ctx.Render<AboutPage>();

        // Assert
        cut.Markup.ShouldContain("aria-busy=\"true\"");
    }

    // ── Error state ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should show error alert when API call fails")]
    public void Render_ShouldShowErrorAlert_WhenApiFails()
    {
        // Arrange
        SetupFailureResponse(System.Net.HttpStatusCode.InternalServerError);

        // Act
        var cut = _ctx.Render<AboutPage>();

        // Assert
        cut.Markup.ShouldContain("Error Loading Sponsors");
    }

    // ── Title sponsor ────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should show title sponsor name when title sponsor exists")]
    public void Render_ShouldShowTitleSponsorName_WhenTitleSponsorExists()
    {
        // Arrange
        var sponsor = SponsorSummaryResponseFactory.Create(name: "Acme Corp", tier: SponsorTier.TitleSponsor);
        SetupSuccessResponse([sponsor]);

        // Act
        var cut = _ctx.Render<AboutPage>();

        // Assert
        cut.Markup.ShouldContain("Title Sponsor");
        cut.Markup.ShouldContain("Acme Corp");
    }

    [Fact(DisplayName = "Should not show title sponsor section when no title sponsor exists")]
    public void Render_ShouldNotShowTitleSponsorSection_WhenNoTitleSponsor()
    {
        // Arrange
        SetupSuccessResponse([]);

        // Act
        var cut = _ctx.Render<AboutPage>();

        // Assert
        cut.Markup.ShouldNotContain("Title Sponsor");
    }

    [Fact(DisplayName = "Should link to title sponsor detail page")]
    public void Render_ShouldLinkToTitleSponsorDetailPage_WhenTitleSponsorExists()
    {
        // Arrange
        var sponsor = SponsorSummaryResponseFactory.Create(slug: "acme-corp", tier: SponsorTier.TitleSponsor);
        SetupSuccessResponse([sponsor]);

        // Act
        var cut = _ctx.Render<AboutPage>();

        // Assert
        cut.Markup.ShouldContain("/sponsors/acme-corp");
    }

    // ── Premier sponsors ─────────────────────────────────────────────────────

    [Fact(DisplayName = "Should show Premier Partners section when premier sponsors exist")]
    public void Render_ShouldShowPremierPartnersSection_WhenPremierSponsorsExist()
    {
        // Arrange
        var sponsor = SponsorSummaryResponseFactory.Create(name: "Premier Co", tier: SponsorTier.Premier);
        SetupSuccessResponse([sponsor]);

        // Act
        var cut = _ctx.Render<AboutPage>();

        // Assert
        cut.Markup.ShouldContain("Premier Partners");
        cut.Markup.ShouldContain("Premier Co");
    }

    [Fact(DisplayName = "Should not show Premier Partners section when no premier sponsors exist")]
    public void Render_ShouldNotShowPremierPartnersSection_WhenNoPremierSponsors()
    {
        // Arrange
        SetupSuccessResponse([]);

        // Act
        var cut = _ctx.Render<AboutPage>();

        // Assert
        cut.Markup.ShouldNotContain("Premier Partners");
    }

    [Fact(DisplayName = "Should not show standard-tier sponsors in the sponsors section")]
    public void Render_ShouldNotShowStandardSponsor_WhenOnlyStandardSponsorExists()
    {
        // Arrange
        var sponsor = SponsorSummaryResponseFactory.Create(name: "Standard Corp", tier: SponsorTier.Standard);
        SetupSuccessResponse([sponsor]);

        // Act
        var cut = _ctx.Render<AboutPage>();

        // Assert
        cut.Markup.ShouldContain("Sponsor information is not currently available.");
        cut.Markup.ShouldNotContain("Standard Corp");
    }

    [Fact(DisplayName = "Should render premier sponsors ordered by priority then name")]
    public void Render_ShouldOrderPremierSponsorsByPriorityThenName()
    {
        // Arrange
        SetupSuccessResponse([
            SponsorSummaryResponseFactory.Create(name: "Zebra Co", priority: 1, tier: SponsorTier.Premier),
            SponsorSummaryResponseFactory.Create(name: "Alpha Co", priority: 2, tier: SponsorTier.Premier),
            SponsorSummaryResponseFactory.Create(name: "Middle Co", priority: 1, tier: SponsorTier.Premier),
        ]);

        // Act
        var cut = _ctx.Render<AboutPage>();
        var markup = cut.Markup;

        // Assert
        markup.IndexOf("Zebra Co", StringComparison.Ordinal)
            .ShouldBeLessThan(markup.IndexOf("Alpha Co", StringComparison.Ordinal));
        markup.IndexOf("Middle Co", StringComparison.Ordinal)
            .ShouldBeLessThan(markup.IndexOf("Zebra Co", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Should show empty state message when no title or premier sponsors exist")]
    public void Render_ShouldShowEmptyState_WhenNoTitleOrPremierSponsors()
    {
        // Arrange
        SetupSuccessResponse([]);

        // Act
        var cut = _ctx.Render<AboutPage>();

        // Assert
        cut.Markup.ShouldContain("Sponsor information is not currently available.");
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