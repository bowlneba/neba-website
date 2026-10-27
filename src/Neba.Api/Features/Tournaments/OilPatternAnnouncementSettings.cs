using System.ComponentModel.DataAnnotations;

namespace Neba.Api.Features.Tournaments;

internal sealed class OilPatternAnnouncementSettings
{
    public const string SectionName = "OilPatternAnnouncement";

    /// <summary>
    /// Seconds between the oil pattern reveal and its announcement, so the page shows the pattern when the
    /// link is clicked. 120 in production; shortened in development.
    /// </summary>
    [Range(0, 3600)]
    public int DelaySeconds { get; init; } = 120;

    public TimeSpan Delay
        => TimeSpan.FromSeconds(DelaySeconds);
}
