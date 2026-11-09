using Microsoft.EntityFrameworkCore;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Discord;
using Neba.Api.Features.News.Domain;
using Neba.Api.Security;
using Neba.Api.Storage;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.News.AnnounceArticleToDiscord;

internal sealed class AnnounceArticleToDiscordJobHandler(
    AppDbContext appDbContext,
    TimeProvider timeProvider,
    WebsiteSettings websiteSettings,
    IFileStorageService fileStorageService,
    INewsAnnouncementNotifier newsAnnouncementNotifier,
    IFusionCache cache,
    ILogger<AnnounceArticleToDiscordJobHandler> logger)
    : IBackgroundJobHandler<AnnounceArticleToDiscordJob>
{
    public async Task ExecuteAsync(AnnounceArticleToDiscordJob job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);

        var articleId = job.ArticleId.Value;

        var row = await appDbContext.Articles
            .AsNoTracking()
            .Where(article => article.Id == job.ArticleId)
            .Select(article => new ArticleRow
            {
                Title = article.Title,
                Slug = article.Slug,
                PublicationStatus = article.PublicationStatus,
                PublishDateUtc = article.PublishDateUtc,
                AnnouncedAtUtc = article.AnnouncedAtUtc,
                HeaderImageContainer = article.HeaderImage != null ? article.HeaderImage.Container : null,
                HeaderImagePath = article.HeaderImage != null ? article.HeaderImage.Path : null
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            logger.LogAnnouncementSkippedMissing(articleId);
            return;
        }

        if (row.PublicationStatus == PublicationStatus.Draft)
        {
            logger.LogAnnouncementSkippedDraft(articleId);
            return;
        }

        if (!IsSameInstant(row.PublishDateUtc, job.ExpectedPublishDateUtc))
        {
            logger.LogAnnouncementSkippedStaleDate(articleId, job.ExpectedPublishDateUtc, row.PublishDateUtc);
            return;
        }

        if (row.AnnouncedAtUtc is not null)
        {
            logger.LogAnnouncementSkippedAlreadyAnnounced(articleId);
            return;
        }

        // Claim before posting, in one atomic statement, so concurrent jobs produce one post. The WHERE
        // clause repeats the checks above, so an edit between the read and this statement still blocks
        // the post. Never undone: a failed post is not retried, because a timeout after Discord accepted
        // it looks the same as a failure. Article.MarkAnnounced is the same rule for in-memory callers.
        var publishDateFloor = job.ExpectedPublishDateUtc - PublishDateTolerance;
        var publishDateCeiling = job.ExpectedPublishDateUtc + PublishDateTolerance;

        var claimed = await appDbContext.Articles
            .Where(article => article.Id == job.ArticleId
                && article.AnnouncedAtUtc == null
                && article.PublicationStatus == PublicationStatus.Published
                && article.PublishDateUtc >= publishDateFloor
                && article.PublishDateUtc <= publishDateCeiling)
            .ExecuteUpdateAsync(
                set => set.SetProperty(article => article.AnnouncedAtUtc, timeProvider.GetUtcNow()),
                cancellationToken);

        if (claimed == 0)
        {
            logger.LogAnnouncementSkippedClaimLost(articleId);
            return;
        }

        var articleUrl = new Uri($"{websiteSettings.BaseUrl.TrimEnd('/')}/news/{row.Slug}");

        Uri? imageUrl = row.HeaderImageContainer is not null && row.HeaderImagePath is not null
            ? fileStorageService.GetBlobUri(row.HeaderImageContainer, row.HeaderImagePath)
            : null;

        await newsAnnouncementNotifier.NotifyAsync(
            new NewsAnnouncement(row.Title, articleUrl, imageUrl),
            cancellationToken);

        // The claim wrote AnnouncedAtUtc straight to the database, so the cached article detail that
        // editors see is stale. Evicted after the post so a failed eviction cannot stop the
        // announcement; a failed post leaves the cache alone.
        await cache.RemoveByTagAsync($"neba:news:{row.Slug}", token: cancellationToken);

        logger.LogArticleAnnounced(articleId);
    }

    // Absorbs sub-millisecond rounding between the date the job was scheduled with and the stored one.
    // A real edit moves the date by far more than this.
    private static readonly TimeSpan PublishDateTolerance = TimeSpan.FromMilliseconds(1);

    private static bool IsSameInstant(DateTimeOffset stored, DateTimeOffset expected)
        => (stored - expected).Duration() <= PublishDateTolerance;

    private sealed class ArticleRow
    {
        public required string Title { get; init; }
        public required string Slug { get; init; }
        public required PublicationStatus PublicationStatus { get; init; }
        public required DateTimeOffset PublishDateUtc { get; init; }
        public DateTimeOffset? AnnouncedAtUtc { get; init; }
        public string? HeaderImageContainer { get; init; }
        public string? HeaderImagePath { get; init; }
    }
}

internal static partial class AnnounceArticleToDiscordJobLogMessages
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Skipped announcing article {ArticleId}: it no longer exists.")]
    public static partial void LogAnnouncementSkippedMissing(this ILogger<AnnounceArticleToDiscordJobHandler> logger, Ulid articleId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Skipped announcing article {ArticleId}: it is a draft.")]
    public static partial void LogAnnouncementSkippedDraft(this ILogger<AnnounceArticleToDiscordJobHandler> logger, Ulid articleId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Skipped announcing article {ArticleId}: the job was scheduled for {ExpectedPublishDateUtc} but the publish date is now {PublishDateUtc}.")]
    public static partial void LogAnnouncementSkippedStaleDate(this ILogger<AnnounceArticleToDiscordJobHandler> logger, Ulid articleId, DateTimeOffset expectedPublishDateUtc, DateTimeOffset publishDateUtc);

    [LoggerMessage(Level = LogLevel.Information, Message = "Skipped announcing article {ArticleId}: it was already announced.")]
    public static partial void LogAnnouncementSkippedAlreadyAnnounced(this ILogger<AnnounceArticleToDiscordJobHandler> logger, Ulid articleId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Skipped announcing article {ArticleId}: another job claimed it, or it changed after the check.")]
    public static partial void LogAnnouncementSkippedClaimLost(this ILogger<AnnounceArticleToDiscordJobHandler> logger, Ulid articleId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Announced article {ArticleId} to Discord.")]
    public static partial void LogArticleAnnounced(this ILogger<AnnounceArticleToDiscordJobHandler> logger, Ulid articleId);
}