using Neba.Api.BackgroundJobs;
using Neba.Api.Features.News.AnnounceArticleToDiscord;
using Neba.Api.Features.News.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.News;

namespace Neba.Api.Tests.Features.News.AnnounceArticleToDiscord;

[UnitTest]
[Component("News")]
public sealed class ArticleAnnouncementSchedulingTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 25, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Delay = TimeSpan.FromMinutes(5);

    private readonly Mock<IBackgroundJobScheduler> _schedulerMock = new(MockBehavior.Strict);

    private sealed class Captured<T>
    {
        public T? Value { get; set; }
    }

    private (Captured<AnnounceArticleToDiscordJob> Job, Captured<DateTimeOffset> At) SetupSchedule()
    {
        var job = new Captured<AnnounceArticleToDiscordJob>();
        var at = new Captured<DateTimeOffset>();

        _schedulerMock
            .Setup(s => s.Schedule(It.IsAny<AnnounceArticleToDiscordJob>(), It.IsAny<DateTimeOffset>()))
            .Callback<AnnounceArticleToDiscordJob, DateTimeOffset>((j, t) =>
            {
                job.Value = j;
                at.Value = t;
            })
            .Returns("job-1");

        return (job, at);
    }

    [Fact(DisplayName = "ScheduleAnnouncement schedules the job at the announcement time for a published article")]
    public void ScheduleAnnouncement_ShouldScheduleJob_WhenArticleIsPublishedAndNotAnnounced()
    {
        // Arrange
        var publishDate = Now.AddHours(-1);
        var article = ArticleFactory.Create(
            publicationStatus: PublicationStatus.Published,
            publishDateUtc: publishDate);

        var (job, at) = SetupSchedule();

        // Act
        _schedulerMock.Object.ScheduleAnnouncement(article, Now, Delay);

        // Assert
        job.Value.ShouldNotBeNull();
        job.Value.ArticleId.ShouldBe(article.Id);
        job.Value.ExpectedPublishDateUtc.ShouldBe(publishDate);
        at.Value.ShouldBe(Now + Delay);
    }

    [Fact(DisplayName = "ScheduleAnnouncement waits for a future publish date")]
    public void ScheduleAnnouncement_ShouldScheduleAtPublishDatePlusDelay_WhenPublishDateIsInTheFuture()
    {
        // Arrange
        var publishDate = Now.AddDays(2);
        var article = ArticleFactory.Create(
            publicationStatus: PublicationStatus.Published,
            publishDateUtc: publishDate);

        var (_, at) = SetupSchedule();

        // Act
        _schedulerMock.Object.ScheduleAnnouncement(article, Now, Delay);

        // Assert
        at.Value.ShouldBe(publishDate + Delay);
    }

    [Fact(DisplayName = "ScheduleAnnouncement truncates the expected publish date to whole microseconds")]
    public void ScheduleAnnouncement_ShouldTruncatePublishDateToMicroseconds()
    {
        // Arrange
        var finePublishDate = Now.AddHours(-1).AddTicks(7);
        var expected = Now.AddHours(-1);
        var article = ArticleFactory.Create(
            publicationStatus: PublicationStatus.Published,
            publishDateUtc: finePublishDate);

        var (job, _) = SetupSchedule();

        // Act
        _schedulerMock.Object.ScheduleAnnouncement(article, Now, Delay);

        // Assert
        job.Value.ShouldNotBeNull();
        job.Value.ExpectedPublishDateUtc.ShouldBe(expected);
        job.Value.ExpectedPublishDateUtc.Ticks.ShouldNotBe(finePublishDate.Ticks);
    }

    [Fact(DisplayName = "ScheduleAnnouncement schedules nothing for a draft article")]
    public void ScheduleAnnouncement_ShouldDoNothing_WhenArticleIsDraft()
    {
        // Arrange
        var article = ArticleFactory.Create(publicationStatus: PublicationStatus.Draft);

        // Act
        _schedulerMock.Object.ScheduleAnnouncement(article, Now, Delay);

        // Assert
        _schedulerMock.Invocations.ShouldBeEmpty();
    }

    [Fact(DisplayName = "ScheduleAnnouncement schedules nothing for an article already announced")]
    public void ScheduleAnnouncement_ShouldDoNothing_WhenArticleIsAnnounced()
    {
        // Arrange
        var article = ArticleFactory.Create(
            publicationStatus: PublicationStatus.Published,
            announcedAtUtc: Now.AddDays(-1));

        // Act
        _schedulerMock.Object.ScheduleAnnouncement(article, Now, Delay);

        // Assert
        _schedulerMock.Invocations.ShouldBeEmpty();
    }

    [Fact(DisplayName = "ScheduleAnnouncement throws when the article is null")]
    public void ScheduleAnnouncement_ShouldThrow_WhenArticleIsNull()
    {
#nullable disable
        // Arrange
        Article article = null;

        // Act
        void Act() => _schedulerMock.Object.ScheduleAnnouncement(article, Now, Delay);

        // Assert
        Should.Throw<ArgumentNullException>(Act);
#nullable enable
    }
}