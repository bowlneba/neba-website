using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Security.ListUsers;

namespace Neba.Api.Security.ListUsers;

internal sealed class ListUsersSummary : Summary<ListUsersEndpoint>
{
    public ListUsersSummary()
    {
        Summary = "Lists user accounts.";
        Description = "Retrieves a paginated list of user accounts ordered by email, including email confirmation status and assigned roles. Requires the System.GetUsers permission.";

        Response(200, "The paginated list of users.",
            contentType: MediaTypeNames.Application.Json,
            example: new PaginationResponse<UserSummaryResponse>
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
                ],
                TotalItems = 1,
                PageNumber = 1,
                PageSize = 20
            });

        Response<Microsoft.AspNetCore.Http.HttpValidationProblemDetails>(400, "The page or pageSize parameter is invalid.");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the System.GetUsers permission.");
    }
}