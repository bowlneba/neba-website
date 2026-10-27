using System.ComponentModel.DataAnnotations;

namespace Neba.Api.Features.News;

internal sealed class NewsAnnouncementSettings
{
    public const string SectionName = "NewsAnnouncement";

    /// <summary>
    /// Seconds between an article becoming public and its announcement, so the page is live when the
    /// link is clicked. 120 in production; shortened in development.
    /// </summary>
    [Range(0, 3600)]
    public int DelaySeconds { get; init; } = 120;

    public TimeSpan Delay
        => TimeSpan.FromSeconds(DelaySeconds);
}