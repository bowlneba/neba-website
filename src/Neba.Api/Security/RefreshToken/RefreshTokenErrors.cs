using ErrorOr;

namespace Neba.Api.Security.RefreshToken;

internal static class RefreshTokenErrors
{
    public static Error InvalidRefreshToken
        => Error.Unauthorized(
            code: "RefreshToken.InvalidRefreshToken",
            description: "The refresh token is invalid or has expired.");
}