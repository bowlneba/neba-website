namespace Neba.Api.Contracts.Security.Register;

/// <summary>
/// Registers a new user account. Day 1: admin-created only; self-registration is Phase 8.
/// </summary>
public sealed record RegisterRequest
{
    /// <summary>
    /// The user account fields to create.
    /// </summary>
    public required RegisterInput Input { get; init; }
}