namespace Neba.Api.Contracts.Security.CreateUser;

/// <summary>A single claim type/value pair to grant a newly created user.</summary>
public sealed record ClaimInput
{
    /// <summary>
    /// The claim type to grant the new user. Must be a valid claim type recognized by the system.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// The claim value to grant the new user.
    /// </summary>
    public required string Value { get; init; }
}