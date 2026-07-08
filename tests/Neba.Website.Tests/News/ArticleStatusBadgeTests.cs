using Bunit;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.News;

namespace Neba.Website.Tests.News;

[UnitTest]
[Component("Website.News.ArticleStatusBadge")]
public sealed class ArticleStatusBadgeTests : IDisposable
{
    private readonly BunitContext _ctx;

    public ArticleStatusBadgeTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should show Draft label and modifier when PublicationStatus is not Published")]
    public void Render_ShouldShowDraftLabelAndModifier_WhenPublicationStatusIsNotPublished()
    {
        // Arrange & Act
        var cut = _ctx.Render<ArticleStatusBadge>(p => p
            .Add(x => x.PublicationStatus, "Draft")
            .Add(x => x.PublishDateUtc, DateTimeOffset.UtcNow.AddDays(-1)));

        // Assert
        cut.Markup.ShouldContain("Draft");
        cut.Find("span.article-status-badge--draft").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should show Scheduled label and modifier when Published but PublishDateUtc is in the future")]
    public void Render_ShouldShowScheduledLabelAndModifier_WhenPublishedWithFuturePublishDate()
    {
        // Arrange & Act
        var cut = _ctx.Render<ArticleStatusBadge>(p => p
            .Add(x => x.PublicationStatus, "Published")
            .Add(x => x.PublishDateUtc, DateTimeOffset.UtcNow.AddDays(3)));

        // Assert
        cut.Markup.ShouldContain("Scheduled");
        cut.Find("span.article-status-badge--scheduled").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should show Published label and modifier when Published and PublishDateUtc is in the past")]
    public void Render_ShouldShowPublishedLabelAndModifier_WhenPublishedWithPastPublishDate()
    {
        // Arrange & Act
        var cut = _ctx.Render<ArticleStatusBadge>(p => p
            .Add(x => x.PublicationStatus, "Published")
            .Add(x => x.PublishDateUtc, DateTimeOffset.UtcNow.AddDays(-3)));

        // Assert
        cut.Markup.ShouldContain("Published");
        cut.Find("span.article-status-badge--published").ShouldNotBeNull();
    }
}
