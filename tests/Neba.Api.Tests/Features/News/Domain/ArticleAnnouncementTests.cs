using Neba.Api.Features.News.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.News;

namespace Neba.Api.Tests.Features.News.Domain;

[UnitTest]
[Component("News")]
public sealed class ArticleAnnouncementTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Delay = TimeSpan.FromMinutes(2);

    [Fact(DisplayName = "GetAnnouncementTime returns null for a draft")]
    public void GetAnnouncementTime_ShouldReturnNull_WhenArticleIsDraft()
    {
        // Arrange
        var article = ArticleFactory.Create(
            publicationStatus: PublicationStatus.Draft,
            publishDateUtc: Now.AddDays(-1));

        // Act
        var result = article.GetAnnouncementTime(Now, Delay);

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "GetAnnouncementTime returns null when the article was already announced")]
    public void GetAnnouncementTime_ShouldReturnNull_WhenArticleAlreadyAnnounced()
    {
        // Arrange
        var article = ArticleFactory.Create(
            publicationStatus: PublicationStatus.Published,
            publishDateUtc: Now.AddDays(-1),
            announcedAtUtc: Now.AddHours(-1));

        // Act
        var result = article.GetAnnouncementTime(Now, Delay);

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "GetAnnouncementTime adds the delay to a future publish date")]
    public void GetAnnouncementTime_ShouldAddDelayToPublishDate_WhenPublishDateIsInFuture()
    {
        // Arrange
        var publishDate = Now.AddDays(3);
        var article = ArticleFactory.Create(
            publicationStatus: PublicationStatus.Published,
            publishDateUtc: publishDate);

        // Act
        var result = article.GetAnnouncementTime(Now, Delay);

        // Assert
        result.ShouldBe(publishDate + Delay);
    }

    [Fact(DisplayName = "GetAnnouncementTime adds the delay to now when the publish date has passed")]
    public void GetAnnouncementTime_ShouldAddDelayToNow_WhenPublishDateIsInPast()
    {
        // Arrange
        var article = ArticleFactory.Create(
            publicationStatus: PublicationStatus.Published,
            publishDateUtc: Now.AddDays(-3));

        // Act
        var result = article.GetAnnouncementTime(Now, Delay);

        // Assert
        result.ShouldBe(Now + Delay);
    }

    [Fact(DisplayName = "GetAnnouncementTime adds the delay to now when the publish date is now")]
    public void GetAnnouncementTime_ShouldAddDelayToNow_WhenPublishDateIsNow()
    {
        // Arrange
        var article = ArticleFactory.Create(
            publicationStatus: PublicationStatus.Published,
            publishDateUtc: Now);

        // Act
        var result = article.GetAnnouncementTime(Now, Delay);

        // Assert
        result.ShouldBe(Now + Delay);
    }

    [Fact(DisplayName = "GetAnnouncementTime returns the later of the publish date and now when the delay is zero")]
    public void GetAnnouncementTime_ShouldReturnLaterOfPublishDateAndNow_WhenDelayIsZero()
    {
        // Arrange
        var publishDate = Now.AddHours(5);
        var article = ArticleFactory.Create(
            publicationStatus: PublicationStatus.Published,
            publishDateUtc: publishDate);

        // Act
        var result = article.GetAnnouncementTime(Now, TimeSpan.Zero);

        // Assert
        result.ShouldBe(publishDate);
    }

    [Fact(DisplayName = "A new article has no announced time")]
    public void AnnouncedAtUtc_ShouldBeNull_WhenArticleIsCreated()
    {
        // Arrange & Act
        var article = ArticleFactory.Create();

        // Assert
        article.AnnouncedAtUtc.ShouldBeNull();
    }

    [Fact(DisplayName = "MarkAnnounced sets the announced time")]
    public void MarkAnnounced_ShouldSetAnnouncedAtUtc()
    {
        // Arrange
        var article = ArticleFactory.Create();

        // Act
        article.MarkAnnounced(Now);

        // Assert
        article.AnnouncedAtUtc.ShouldBe(Now);
    }

    [Fact(DisplayName = "MarkAnnounced keeps the first announced time on a second call")]
    public void MarkAnnounced_ShouldKeepFirstTimestamp_WhenCalledTwice()
    {
        // Arrange
        var article = ArticleFactory.Create();
        article.MarkAnnounced(Now);

        // Act
        article.MarkAnnounced(Now.AddHours(1));

        // Assert
        article.AnnouncedAtUtc.ShouldBe(Now);
    }
}