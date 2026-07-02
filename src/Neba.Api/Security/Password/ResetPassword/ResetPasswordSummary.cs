using FastEndpoints;

namespace Neba.Api.Security.Password.ResetPassword;

internal sealed class ResetPasswordSummary : Summary<ResetPasswordEndpoint>
{
    public ResetPasswordSummary()
    {
        Summary = "Resets a user's password (admin only).";
        Description = "Generates a secure temporary password server-side, replaces the user's current password, and emails the temporary password directly to the user. The admin never sees the credential. Requires the Admin role.";

        Response(204, "Password reset successfully — temporary password emailed to the user.");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the Admin role.");
        Response(404, "No user found with the given user ID.");
        Response(422, "Validation failed (missing or invalid user ID format).");
    }
}