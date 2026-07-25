using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Tournaments.ListOilPatterns;

/// <summary>
/// Represents a summary of an oil pattern.
/// </summary>
public sealed record OilPatternSummaryDto
{
    /// <summary>
    /// Gets the unique identifier for the oil pattern.
    /// </summary>
    public required OilPatternId Id { get; init; }

    /// <summary>
    /// Gets the name of the oil pattern.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the length of the oil pattern.
    /// </summary>
    public required int Length { get; init; }

    /// <summary>
    /// Gets the volume of the oil pattern.
    /// </summary>
    public required decimal Volume { get; init; }

    /// <summary>
    /// Gets the left ratio of the oil pattern.
    /// </summary>
    public required decimal LeftRatio { get; init; }

    /// <summary>
    /// Gets the right ratio of the oil pattern.
    /// </summary>
    public required decimal RightRatio { get; init; }

    /// <summary>
    /// Gets the unique identifier for the Kegel associated with the oil pattern.
    /// </summary>
    public Guid? KegelId { get; init; }

    /// <summary>
    /// Gets the length category of the oil pattern.
    /// </summary>
    public required string LengthCategory { get; init; }

    /// <summary>
    /// Gets the ratio category of the oil pattern.
    /// </summary>
    public required string RatioCategory { get; init; }
}