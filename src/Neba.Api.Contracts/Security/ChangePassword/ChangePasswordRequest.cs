using Neba.Api.Contracts.Compliance;

namespace Neba.Api.Contracts.Security.ChangePassword;

/// <summary>
/// Changes the signed-in user's own password.
/// </summary>
public sealed record ChangePasswordRequest
{
    /// <summary>
    /// The user's current password.
    /// </summary>
    [PrivateData]
    public required string CurrentPassword { get; init; }

    /// <summary>
    /// The new password.
    /// </summary>
    [PrivateData]
    public required string NewPassword { get; init; }
}
