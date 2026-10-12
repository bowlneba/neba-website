using ErrorOr;

namespace Neba.Api.Security.GetUser;

internal static class GetUserErrors
{
    public static Error UserNotFound
        => Error.NotFound("GetUser.UserNotFound", "No user was found with the given ID.");
}
