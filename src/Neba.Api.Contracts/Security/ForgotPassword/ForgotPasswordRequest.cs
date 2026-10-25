using Neba.Api.Contracts.Compliance;

namespace Neba.Api.Contracts.Security.ForgotPassword;

/// <summary>
/// Asks for a password reset link to be emailed to an account.
/// </summary>
public sealed record ForgotPasswordRequest
{
    /// <summary>
    /// The email address of the account.
    /// </summary>
    [PersonalData]
    public required string Email { get; init; }
}