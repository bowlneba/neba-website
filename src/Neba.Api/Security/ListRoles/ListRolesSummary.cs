using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.Security.ListRoles;

namespace Neba.Api.Security.ListRoles;

internal sealed class ListRolesSummary : Summary<ListRolesEndpoint>
{
    public ListRolesSummary()
    {
        Summary = "Lists roles with their permissions.";
        Description = "Returns every role, ordered by name, with the permission values each one grants. Requires the System.GetUsers permission.";

        Response(200, "The roles.",
            contentType: MediaTypeNames.Application.Json,
            example: new ListRolesResponse
            {
                Roles =
                [
                    new RoleResponse
                    {
                        Name = "Tournament Director",
                        Permissions = ["Tournaments.CreateTournament", "Tournaments.EditTournament"]
                    }
                ]
            });

        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the System.GetUsers permission.");
    }
}
