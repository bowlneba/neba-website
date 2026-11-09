using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.Documents;
using Neba.Api.Contracts.Documents.GetDocument;
using Neba.TestFactory.Attributes;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Services;
using Neba.Website.Server.Time;
using Neba.Website.Server.Tournaments.Detail;

using Refit.Testing;

namespace Neba.Website.Tests.Tournaments.Detail;

[UnitTest]
[Component("Website.Tournaments.Detail.TournamentRulesLink")]
public sealed class TournamentRulesLinkTests : IDisposable
{
    private const string Html =
        "<h3 id=\"rule-601-invitational-tournament-rules\">Rule 601- Invitational Tournament Rules</h3><p>Eligibility</p>" +
        "<h3 id=\"rule-602-masters-tournament-rules\">Rule 602 - Masters Tournament Rules</h3><p>Masters text</p>";
    private const string HtmlWithoutInvitational =
        "<h3 id=\"rule-100-general\">Rule 100 - General</h3><p>General text</p>";

    private readonly BunitContext _ctx = new();
    private readonly Mock<IDocumentsApi> _mockDocumentsApi = new(MockBehavior.Strict);
    private readonly Mock<IClientTimeZoneService> _mockClientTimeZoneService = new(MockBehavior.Strict);

    public TournamentRulesLinkTests()
    {
        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddSingleton(_mockDocumentsApi.Object);
        _ctx.Services.AddSingleton(_mockClientTimeZoneService.Object);
        _ctx.Services.AddSingleton(sp => new ApiExecutor(mockStopwatch.Object, sp.GetRequiredService<NavigationManager>(), NullLogger<ApiExecutor>.Instance));
    }

    public void Dispose() => _ctx.Dispose();

    private void SetupDocument(bool success = true, string? html = null)
    {
        using var response = new StubApiResponse<GetDocumentResponse>
        {
            IsSuccessStatusCode = success,
            Content = success ? new GetDocumentResponse { Html = html ?? Html } : null,
            StatusCode = success ? System.Net.HttpStatusCode.OK : System.Net.HttpStatusCode.InternalServerError
        };

        _mockDocumentsApi
            .Setup(x => x.GetDocumentAsync("tournament-rules", It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private IRenderedComponent<TournamentRulesLink> RenderLink(string type = "Invitational")
        => _ctx.Render<TournamentRulesLink>(p => p.Add(x => x.TournamentType, type));

    [Fact(DisplayName = "Renders nothing for a type without a rule section")]
    public void Render_ShouldRenderNothing_WhenTypeUnmapped()
    {
        // Arrange & Act
        var cut = RenderLink("Senior");

        // Assert
        cut.Markup.Trim().ShouldBeEmpty();
    }

    [Fact(DisplayName = "Opening the panel fetches the rules once and shows the extracted section")]
    public async Task Open_ShouldShowSection_WhenExtractionSucceeds()
    {
        // Arrange
        SetupDocument();
        var cut = RenderLink();

        // Act
        await cut.Find("button.td-rules-link").ClickAsync(new MouseEventArgs());
        await cut.Find(".neba-document-slideover-close").ClickAsync(new MouseEventArgs());
        await cut.Find("button.td-rules-link").ClickAsync(new MouseEventArgs());

        // Assert
        cut.Find(".neba-document-slideover-content").TextContent.ShouldContain("Eligibility");
        cut.Find(".neba-document-slideover-content").TextContent.ShouldNotContain("Masters text");
        cut.Find(".neba-document-slideover-footer a").GetAttribute("href").ShouldBe("/tournaments/rules#invitational");
        _mockDocumentsApi.Verify(x => x.GetDocumentAsync("tournament-rules", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "API error shows the could-not-load message and keeps the full-page link")]
    public async Task Open_ShouldShowLoadError_WhenApiFails()
    {
        // Arrange
        SetupDocument(success: false);
        var cut = RenderLink();

        // Act
        await cut.Find("button.td-rules-link").ClickAsync(new MouseEventArgs());

        // Assert
        cut.Find(".neba-document-slideover-status").TextContent.ShouldContain("couldn't load the rules");
        cut.Find(".neba-document-slideover-footer a").GetAttribute("href").ShouldBe("/tournaments/rules#invitational");
    }

    [Fact(DisplayName = "A document without the section shows the could-not-find message")]
    public async Task Open_ShouldShowNotFound_WhenSectionMissing()
    {
        // Arrange
        SetupDocument(html: HtmlWithoutInvitational);
        var cut = RenderLink();

        // Act
        await cut.Find("button.td-rules-link").ClickAsync(new MouseEventArgs());

        // Assert
        cut.Find(".neba-document-slideover-status").TextContent.ShouldContain("couldn't find this section");
    }
}