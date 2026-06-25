using ErrorOr;

namespace Neba.Api.Security.Login;

internal static class LoginErrors
{
    public static Error InvalidCredentials
        => Error.Unauthorized(
            code: "Login.InvalidCredentials",
            description: "The email or password is incorrect.");
}