using ErrorOr;

namespace Neba.Api.Security.Password.ChangePassword;

internal static class ChangePasswordErrors
{
    public static Error UserNotFound =>
        Error.NotFound("Security.UserNotFound", "No user was found.");

    public static Error IncorrectCurrentPassword =>
        Error.Validation("ChangePassword.IncorrectCurrentPassword", "Current password is incorrect.");

    public static Error PasswordUnchanged =>
        Error.Validation("ChangePassword.PasswordUnchanged", "New password must be different from the current password.");

    // Conflict, not Validation: the same payload succeeds once the lockout ends.
    public static Error LockedOut =>
        Error.Conflict("ChangePassword.LockedOut", "Too many incorrect attempts. Try again in a few minutes.");
}