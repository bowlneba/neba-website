namespace Neba.Website.Server.Tournaments;

/// <summary>
/// The oil pattern choice emitted by <see cref="OilPatternPicker"/>: either an existing pattern's ID,
/// manually-entered categories, or neither.
/// </summary>
public sealed record OilPatternSelection
{
    /// <summary>The ID of the existing oil pattern picked, if any.</summary>
    public string? OilPatternId { get; init; }

    /// <summary>The manually-entered pattern length category, if no pattern was picked.</summary>
    public string? PatternLengthCategory { get; init; }

    /// <summary>The manually-entered pattern ratio category, if no pattern was picked.</summary>
    public string? PatternRatioCategory { get; init; }
}
