using Neba.Api.BackgroundJobs;
using Neba.Api.Features.News.Domain;

namespace Neba.Api.Features.News.AnnounceArticleToDiscord;

internal static class ArticleAnnouncementScheduling
{
    // PostgreSQL stores timestamps to the microsecond, but a publish date saved from the API can carry
    // finer ticks. The job compares the date it was scheduled for with the stored one, so schedule with
    // the value the database will return.
    private const long TicksPerMicrosecond = 10;

    extension(IBackgroundJobScheduler scheduler)
    {
        /// <summary>
        /// Schedules the announcement job when <paramref name="article"/> is due one. Does nothing for a
        /// draft or an announced article.
        /// </summary>
        public void ScheduleAnnouncement(Article article, DateTimeOffset now, TimeSpan delay)
        {
            ArgumentNullException.ThrowIfNull(article);

            var announceAt = article.GetAnnouncementTime(now, delay);

            if (announceAt is null)
            {
                return;
            }

            scheduler.Schedule(
                new AnnounceArticleToDiscordJob
                {
                    ArticleId = article.Id,
                    ExpectedPublishDateUtc = TruncateToMicroseconds(article.PublishDateUtc)
                },
                announceAt.Value);
        }
    }

    private static DateTimeOffset TruncateToMicroseconds(DateTimeOffset value)
        => new(value.Ticks - (value.Ticks % TicksPerMicrosecond), value.Offset);
}
