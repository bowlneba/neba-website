namespace Neba.Api.Contracts.OilPatterns.CreateOilPattern;

/// <summary>
/// Result of successfully creating an oil pattern, including its derived lane-condition categories.
/// </summary>
public sealed record CreatedOilPatternResponse
{
    /// <summary>The unique identifier of the newly created oil pattern.</summary>
    public required string OilPatternId { get; init; }

    /// <summary>The pattern's name.</summary>
    public required string Name { get; init; }

    /// <summary>The pattern's length, in feet.</summary>
    public required int Length { get; init; }

    /// <summary>
    /// The length category name (see <c>PatternLengthCategory</c>), derived from <see cref="Length"/>.
    /// </summary>
    public required string LengthCategory { get; init; }

    /// <summary>
    /// The ratio category name (see <c>PatternRatioCategory</c>), derived from the harder side's ratio.
    /// </summary>
    public required string RatioCategory { get; init; }
}
