namespace Neba.Api.Contracts.OilPatterns.ListOilPatterns;

/// <summary>
/// Represents a summary of an oil pattern for display in lists and pickers.
/// </summary>
public sealed record OilPatternSummaryResponse
{
    /// <summary>
    /// The unique identifier for the oil pattern.
    /// </summary>
    public required string OilPatternId { get; init; }

    /// <summary>
    /// The name of the oil pattern.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The length, in feet, of the oil pattern.
    /// </summary>
    public required int Length { get; init; }

    /// <summary>
    /// The volume, in milliliters, of the oil pattern.
    /// </summary>
    public required decimal Volume { get; init; }

    /// <summary>
    /// The left-side ratio of the oil pattern.
    /// </summary>
    public required decimal LeftRatio { get; init; }

    /// <summary>
    /// The right-side ratio of the oil pattern.
    /// </summary>
    public required decimal RightRatio { get; init; }

    /// <summary>
    /// The Kegel identifier for the pattern, if known.
    /// </summary>
    public Guid? KegelId { get; init; }

    /// <summary>
    /// The length category derived from <see cref="Length"/>.
    /// </summary>
    public required string LengthCategory { get; init; }

    /// <summary>
    /// The ratio category derived from the harder of <see cref="LeftRatio"/>/<see cref="RightRatio"/>.
    /// </summary>
    public required string RatioCategory { get; init; }
}