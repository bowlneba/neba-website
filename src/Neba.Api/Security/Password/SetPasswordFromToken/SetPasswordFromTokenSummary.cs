using FastEndpoints;

namespace Neba.Api.Security.Password.SetPasswordFromToken;

internal sealed class SetPasswordFromTokenSummary : Summary<SetPasswordFromTokenEndpoint>
{
    public SetPasswordFromTokenSummary()
    {
        Summary = "Sets a new password using a token, and confirms the user's email.";
        Description = "Anonymous, token-based endpoint used by invite links, admin-triggered resets, and (eventually) forgot-password. Successfully consuming the token proves ownership of the email address, so EmailConfirmed is set to true in the same operation. An unknown user id and an invalid/expired token are indistinguishable in the response, to prevent user-id enumeration.";

        Response(204, "Password was set successfully and the account is now email-confirmed.");
        Response(422, "The token is invalid or expired, the user id is unrecognized, or the new password failed validation.");
    }
}
