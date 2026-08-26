using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Security.ListUsers;

namespace Neba.Api.Security.ListUsers;

internal sealed class ListUsersSummary : Summary<ListUsersEndpoint>
{
    public ListUsersSummary()
    {
        Summary = "Lists all user accounts.";
        Description = "Retrieves a summary of every user account, including email confirmation status and assigned roles. Requires the System.ResetUserPassword permission.";

        Response(200, "The list of users.",
            contentType: MediaTypeNames.Application.Json,
            example: new CollectionResponse<UserSummaryResponse>
            {
                Items =
                [
                    new UserSummaryResponse
                    {
                        UserId = "01JXXXXXXXXXXXXXXXXXXXXXXXXX",
                        Email = "webmaster@bowlneba.com",
                        EmailConfirmed = true,
                        Roles = ["Webmaster"]
                    }
                ]
            });

        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the System.ResetUserPassword permission.");
    }
}