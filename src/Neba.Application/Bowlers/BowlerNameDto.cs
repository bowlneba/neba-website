namespace Neba.Application.Bowlers;

/// <summary>
/// Data transfer object representing a bowler's name. Uses primitive types to ensure cache serialization compatibility.
/// </summary>
public sealed record BowlerNameDto
{
    /// <summary>
    /// The bowler's given first name.
    /// </summary>
    public required string FirstName { get; init; }

    /// <summary>
    /// The bowler's family or surname.
    /// </summary>
    public required string LastName { get; init; }

    /// <summary>
    /// The bowler's middle name (optional).
    /// </summary>
    public string? MiddleName { get; init; }

    /// <summary>
    /// Name suffix such as Jr., Sr., II, III, IV, or V (optional).
    /// </summary>
    public string? Suffix { get; init; }

    /// <summary>
    /// The bowler's preferred informal name (optional).
    /// </summary>
    public string? Nickname { get; init; }
}