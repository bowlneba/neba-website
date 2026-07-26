using ErrorOr;

namespace Neba.Api.Security.Password.SetPasswordFromToken;

internal static class SetPasswordFromTokenErrors
{
    public static Error InvalidOrExpiredToken =>
        Error.Validation("Security.InvalidOrExpiredToken", "This link is invalid or has expired.");
}