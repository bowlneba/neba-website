using System.ComponentModel.DataAnnotations;

namespace Neba.Api.Features.Series;

internal sealed class SeriesAdvancementSettings
{
    public const string SectionName = "SeriesAdvancement";

    /// <summary>
    /// Hours between a Stage being computed and it becoming Official automatically. Must be positive. 24 in production.
    /// </summary>
    [Range(1, 168)]
    public int OfficialGraceHours { get; init; } = 24;

    public TimeSpan OfficialGrace
        => TimeSpan.FromHours(OfficialGraceHours);
}