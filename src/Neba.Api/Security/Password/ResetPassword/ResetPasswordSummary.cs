using FastEndpoints;

namespace Neba.Api.Security.Password.ResetPassword;

internal sealed class ResetPasswordSummary : Summary<ResetPasswordEndpoint>
{
    public ResetPasswordSummary()
    {
        Summary = "Resets a user's password by emailing them a password-set link.";
        Description = "Generates a password-set token, emails the user a link to choose a new password, "
                      + "and invalidates their current password immediately. Requires the System.ResetUserPassword permission.";

        Response(204, "Password reset initiated — a set-password link was emailed to the user.");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the System.ResetUserPassword permission.");
        Response(404, "No user found with the given user ID.");
        Response(422, "Validation failed (missing or invalid user ID format).");
    }
}