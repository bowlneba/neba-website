using FastEndpoints;

using Neba.Api.Contracts.Security.ChangePassword;

namespace Neba.Api.Security.Password.ChangePassword;

internal sealed class ChangePasswordSummary : Summary<ChangePasswordEndpoint>
{
    public ChangePasswordSummary()
    {
        Summary = "Changes the signed-in user's own password.";
        Description = "Checks the current password (wrong guesses count toward the same lockout as login), sets the new one, "
                      + "revokes every other session's refresh token, and returns a fresh token pair for this session. "
                      + "Emails the account a notice.";

        Response<ChangePasswordResponse>(200, "Password changed. Replace the stored tokens with the ones returned.");
        Response(400, "Validation failed (missing fields, weak password, or new password equals the current one).");
        Response(401, "No valid bearer token provided.");
        Response(409, "Too many incorrect attempts; the account is temporarily locked.");
        Response(422, "The current password is incorrect, or the new password breaks the password policy.");
    }
}