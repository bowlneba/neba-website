namespace Neba.Api.Contracts.ReferenceData;

/// <summary>
/// Represents a single US state (or the District of Columbia) as a display name / postal-code pair, for populating state dropdowns.
/// </summary>
public sealed record UsStateResponse
{
    /// <summary>
    /// The state's full display name (e.g. "Massachusetts").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The state's postal abbreviation (e.g. "MA").
    /// </summary>
    public required string Code { get; init; }
}