namespace Neba.Api.Contracts.Security.ResetPassword;

/// <summary>
/// Represents a request to reset a user's password.
/// </summary>
public sealed record ResetPasswordRequest
{
    /// <summary>
    /// The user id of the user that is requesting a password reset.
    /// </summary>
    public required string UserId { get; init; }
}