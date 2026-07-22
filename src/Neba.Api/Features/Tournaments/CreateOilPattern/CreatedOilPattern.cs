using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Tournaments.CreateOilPattern;

/// <summary>
/// Result of successfully creating an oil pattern, including its derived lane-condition categories.
/// </summary>
public sealed record CreatedOilPattern
{
    /// <summary>
    /// Gets the unique identifier for the newly created oil pattern.
    /// </summary>
    public required OilPatternId Id { get; init; }

    /// <summary>
    /// Gets the name of the newly created oil pattern.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the length of the newly created oil pattern.
    /// </summary>
    public required int Length { get; init; }

    /// <summary>
    /// Gets the length category of the newly created oil pattern.
    /// </summary>
    public required PatternLengthCategory LengthCategory { get; init; }

    /// <summary>
    /// Gets the ratio category of the newly created oil pattern.
    /// </summary>
    public required PatternRatioCategory RatioCategory { get; init; }
}