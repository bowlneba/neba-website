using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.Security.GetCurrentUser;

namespace Neba.Api.Security.GetCurrentUser;

internal sealed class GetCurrentUserSummary : Summary<GetCurrentUserEndpoint>
{
    public GetCurrentUserSummary()
    {
        Summary = "Returns the current user's profile.";
        Description = "Reads live data from Identity — not cached. Use after login to confirm roles or check USBC ID linkage.";

#pragma warning disable S1075
        Response(200, "Profile retrieved.",
            contentType: MediaTypeNames.Application.Json,
            example: new MeResponse
            {
                UserId = "01JXXXXXXXXXXXXXXXXXXXXXXXXX",
                Email = "admin@bowlneba.com",
                Roles = ["Admin"],
                UsbcId = null,
            });
#pragma warning restore S1075

        Response(401, "No valid bearer token provided.");
        Response(404, "Authenticated user ID not found in Identity (should not occur in normal operation).");
    }
}