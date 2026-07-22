namespace Neba.Api.Contracts.OilPatterns.CreateOilPattern;

/// <summary>
/// The fields required to create an oil pattern.
/// </summary>
public sealed record CreateOilPatternRequest
{
    /// <summary>The pattern's name.</summary>
    public required string Name { get; init; }

    /// <summary>The pattern's length, in feet.</summary>
    public required int Length { get; init; }

    /// <summary>The total oil volume applied, in milliliters.</summary>
    public required decimal Volume { get; init; }

    /// <summary>The left-side ratio.</summary>
    public required decimal LeftRatio { get; init; }

    /// <summary>The right-side ratio.</summary>
    public required decimal RightRatio { get; init; }

    /// <summary>
    /// The Kegel pattern catalog identifier, if this pattern corresponds to one. Must be unique when provided.
    /// </summary>
    public Guid? KegelId { get; init; }
}