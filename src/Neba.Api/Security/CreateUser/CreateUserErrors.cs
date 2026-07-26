using ErrorOr;

namespace Neba.Api.Security.CreateUser;

internal static class CreateUserErrors
{
    public static Error DuplicateEmail
        => Error.Conflict("CreateUser.DuplicateEmail", "An account with this email already exists.");
}