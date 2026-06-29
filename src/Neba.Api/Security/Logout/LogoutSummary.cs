using FastEndpoints;

namespace Neba.Api.Security.Logout;

internal sealed class LogoutSummary : Summary<LogoutEndpoint>
{
    public LogoutSummary()
    {
        Summary = "Logs the current user out.";
        Description = "Revokes the stored refresh token for the authenticated user. The access token remains valid until it expires (15 min). Always returns 204, even if the user was already logged out.";

        Response(204, "Logout successful — refresh token revoked.");
        Response(401, "No valid bearer token provided.");
    }
}
