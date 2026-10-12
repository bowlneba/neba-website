using FastEndpoints;

namespace Neba.Api.Security.UpdateUser;

internal sealed class UpdateUserSummary : Summary<UpdateUserEndpoint>
{
    public UpdateUserSummary()
    {
        Summary = "Updates a user account.";
        Description = "Replaces a user's permission overrides: permissions added regardless of role and permissions removed even when a role grants them. Changes apply when the user's access token next refreshes. Requires the System.EditUser permission. The caller may not edit their own overrides and may only add or restore permissions they hold. Admin accounts can't carry overrides (409).";

        Response(204, "The overrides were replaced.");
        Response(400, "The request was malformed (invalid user ID, unknown or duplicate permission, or a permission in both sets).");
        Response(401, "No valid bearer token provided.");
        Response(403, "The caller does not hold the System.EditUser permission, is editing their own account, or tried to add or restore a permission they don't hold.");
        Response(404, "No user exists with the given ID.");
        Response(409, "The target user is an Admin; overrides don't apply to Admin accounts.");
        Response(422, "The overrides failed domain validation.");
    }
}
