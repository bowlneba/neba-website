namespace Neba.Api.ReferenceData.ListUsStates;

/// <summary>
/// Represents a single US state (or the District of Columbia) as a display name / postal-code pair.
/// </summary>
public sealed record UsStateDto
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
