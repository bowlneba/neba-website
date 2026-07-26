namespace Neba.Api.Contracts.Security.SetPasswordFromToken;

/// <summary>
/// Sets a new password for a user, proving ownership via a token received by email
/// (invite, admin reset, or forgot-password). Confirms the user's email in the same operation.
/// </summary>
public sealed record SetPasswordFromTokenRequest
{
    /// <summary>The id of the user the token was issued for.</summary>
    public required string UserId { get; init; }

    /// <summary>The opaque token issued by <c>UserManager.GeneratePasswordResetTokenAsync</c>.</summary>
    public required string Token { get; init; }

    /// <summary>The new password to set.</summary>
    public required string NewPassword { get; init; }
}