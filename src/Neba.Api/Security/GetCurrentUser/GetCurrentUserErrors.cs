using ErrorOr;

namespace Neba.Api.Security.GetCurrentUser;

internal static class GetCurrentUserErrors
{
    public static Error UserNotFound =>
        Error.NotFound("Security.UserNotFound", "No user was found.");
}