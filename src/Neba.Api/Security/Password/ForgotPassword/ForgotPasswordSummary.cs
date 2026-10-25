using FastEndpoints;

namespace Neba.Api.Security.Password.ForgotPassword;

internal sealed class ForgotPasswordSummary : Summary<ForgotPasswordEndpoint>
{
    public ForgotPasswordSummary()
    {
        Summary = "Emails a password reset link if an account exists.";
        Description = "Always returns 204 whether or not an account exists for the email, to avoid user enumeration. "
                      + "When an account exists, a link valid for 1 hour is emailed (at most one every 2 minutes per account). "
                      + "Rate limited per client IP.";

        Response(204, "Request accepted. If an account exists, a reset link was queued.");
        Response(400, "Validation failed (missing, malformed, or too long email).");
        Response(429, "Too many requests from this client.");
    }
}
