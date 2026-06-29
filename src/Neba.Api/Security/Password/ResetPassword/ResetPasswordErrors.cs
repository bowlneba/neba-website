using ErrorOr;

namespace Neba.Api.Security.Password.ResetPassword;

internal static class ResetPasswordErrors
{
    public static Error UserNotFound =>
        Error.NotFound("Security.UserNotFound", "No user was found.");
}