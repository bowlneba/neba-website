namespace Neba.Api.Contracts.Security;

/// <summary>
/// Which kind of emailed link a password token came from.
/// </summary>
public static class PasswordTokenPurpose
{
    /// <summary>
    /// Forgot-password or admin reset link (valid for 1 hour).
    /// </summary>
    public const string Reset = "reset";

    /// <summary>
    /// New-user invite link (valid for 1 day).
    /// </summary>
    public const string Invite = "invite";
}
