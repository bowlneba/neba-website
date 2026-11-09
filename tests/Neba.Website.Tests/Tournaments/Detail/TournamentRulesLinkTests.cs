using Bunit;

using ErrorOr;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Documents;
using Neba.Website.Server.Tournaments.Detail;

namespace Neba.Website.Tests.Tournaments.Detail;

[UnitTest]
[Component("Website.Tournaments.Detail.TournamentRulesLink")]
public sealed class TournamentRulesLinkTests : IDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly Mock<IRulesSectionService> _mockRulesSectionService = new(MockBehavior.Strict);

    public TournamentRulesLinkTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddSingleton(_mockRulesSectionService.Object);
    }

    public void Dispose() => _ctx.Dispose();

    private void SetupSection(ErrorOr<RulesSection> result)
        => _mockRulesSectionService
            .Setup(x => x.GetSectionAsync(
                It.Is<TournamentRulesAnchor>(a => a.Slug == "invitational"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

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

    [Fact(DisplayName = "Opening the panel fetches the section once and shows it")]
    public async Task Open_ShouldShowSection_WhenServiceSucceeds()
    {
        // Arrange
        SetupSection(new RulesSection("<p>Eligibility</p>", null));
        var cut = RenderLink();

        // Act
        await cut.Find("button.td-rules-link").ClickAsync(new MouseEventArgs());
        await cut.Find(".neba-document-slideover-close").ClickAsync(new MouseEventArgs());
        await cut.Find("button.td-rules-link").ClickAsync(new MouseEventArgs());

        // Assert — a second call would fail: the Strict mock's Setup is the only allowed interaction
        // and the content stays cached after the first load.
        cut.Find(".neba-document-slideover-content").TextContent.ShouldContain("Eligibility");
        cut.Find(".neba-document-slideover-footer a").GetAttribute("href").ShouldBe("/tournaments/rules#invitational");
        _mockRulesSectionService.Invocations.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "A service failure shows the could-not-load message and keeps the full-page link")]
    public async Task Open_ShouldShowLoadError_WhenServiceFails()
    {
        // Arrange
        SetupSection(Error.Failure("Documents.Failed", "boom"));
        var cut = RenderLink();

        // Act
        await cut.Find("button.td-rules-link").ClickAsync(new MouseEventArgs());

        // Assert
        cut.Find(".neba-document-slideover-status").TextContent.ShouldContain("couldn't load the rules");
        cut.Find(".neba-document-slideover-footer a").GetAttribute("href").ShouldBe("/tournaments/rules#invitational");
    }

    [Fact(DisplayName = "A NotFound error shows the could-not-find message")]
    public async Task Open_ShouldShowNotFound_WhenSectionMissing()
    {
        // Arrange
        SetupSection(Error.NotFound("RulesSection.NotFound", "missing"));
        var cut = RenderLink();

        // Act
        await cut.Find("button.td-rules-link").ClickAsync(new MouseEventArgs());

        // Assert
        cut.Find(".neba-document-slideover-status").TextContent.ShouldContain("couldn't find this section");
    }

    [Fact(DisplayName = "A thrown exception shows the could-not-load message and allows a retry")]
    public async Task Open_ShouldShowLoadErrorAndRetry_WhenServiceThrows()
    {
        // Arrange
        var calls = 0;
        _mockRulesSectionService
            .Setup(x => x.GetSectionAsync(It.IsAny<TournamentRulesAnchor>(), It.IsAny<CancellationToken>()))
            .Returns(() => ++calls == 1
                ? throw new InvalidOperationException("circuit dropped")
                : Task.FromResult<ErrorOr<RulesSection>>(new RulesSection("<p>Eligibility</p>", null)));
        var cut = RenderLink();

        // Act
        await cut.Find("button.td-rules-link").ClickAsync(new MouseEventArgs());
        var errorShown = cut.Find(".neba-document-slideover-status").TextContent;
        await cut.Find(".neba-document-slideover-close").ClickAsync(new MouseEventArgs());
        await cut.Find("button.td-rules-link").ClickAsync(new MouseEventArgs());

        // Assert
        errorShown.ShouldContain("couldn't load the rules");
        cut.Find(".neba-document-slideover-content").TextContent.ShouldContain("Eligibility");
    }
}
