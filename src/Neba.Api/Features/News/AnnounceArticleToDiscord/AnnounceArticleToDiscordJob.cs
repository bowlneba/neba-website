using Neba.Api.BackgroundJobs;
using Neba.Api.Features.News.Domain;

namespace Neba.Api.Features.News.AnnounceArticleToDiscord;

/// <summary>
/// Posts a news article to the Discord news channel. Never retried: a retry after a timeout could post twice.
/// </summary>
[NoAutomaticRetry]
public sealed record AnnounceArticleToDiscordJob
    : IBackgroundJob
{
    /// <summary>
    /// Gets the article to announce.
    /// </summary>
    public required ArticleId ArticleId { get; init; }

    /// <summary>
    /// Gets the publish date this job was scheduled for. The job exits if the article's date has since changed.
    /// </summary>
    public required DateTimeOffset ExpectedPublishDateUtc { get; init; }

    /// <inheritdoc />
    public string JobName
        => $"{nameof(AnnounceArticleToDiscordJob)}: article {ArticleId.Value}";
}