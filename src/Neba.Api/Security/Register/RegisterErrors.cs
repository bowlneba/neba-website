using ErrorOr;

namespace Neba.Api.Security.Register;

internal static class RegisterErrors
{
    public static Error DuplicateEmail
        => Error.Conflict("Register.DuplicateEmail", "An account with this email already exists.");
}