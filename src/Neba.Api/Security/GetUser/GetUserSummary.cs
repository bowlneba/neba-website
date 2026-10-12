using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.Security.GetUser;

namespace Neba.Api.Security.GetUser;

internal sealed class GetUserSummary : Summary<GetUserEndpoint>
{
    public GetUserSummary()
    {
        Summary = "Gets a user account.";
        Description = "Returns one user with email confirmation status, assigned roles and the permission overrides (added and removed) set on the account. Requires the System.GetUsers permission.";

        Response(200, "The user.",
            contentType: MediaTypeNames.Application.Json,
            example: new UserDetailResponse
            {
                UserId = "01JXXXXXXXXXXXXXXXXXXXXXXXXX",
                Email = "director@bowlneba.com",
                EmailConfirmed = true,
                Roles = ["Tournament Director"],
                AddedPermissions = ["Sponsors.CreateSponsor"],
                RemovedPermissions = ["Tournaments.DeleteTournament"]
            });

        Response<HttpValidationProblemDetails>(400, "The user ID is not a valid ULID.");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the System.GetUsers permission.");
        Response(404, "No user exists with the given ID.");
    }
}
