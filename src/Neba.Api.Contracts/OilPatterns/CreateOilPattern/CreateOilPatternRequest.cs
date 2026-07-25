namespace Neba.Api.Contracts.OilPatterns.CreateOilPattern;

/// <summary>
/// Creates an oil pattern.
/// </summary>
public sealed record CreateOilPatternRequest
{
    /// <summary>
    /// The oil pattern fields to create.
    /// </summary>
    public required OilPatternInput OilPattern { get; init; }
}