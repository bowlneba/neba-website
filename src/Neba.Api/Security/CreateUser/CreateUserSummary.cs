using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.Security.CreateUser;

namespace Neba.Api.Security.CreateUser;

internal sealed class CreateUserSummary : Summary<CreateUserEndpoint>
{
    public CreateUserSummary()
    {
        Summary = "Creates a new staff user account.";
        Description = "Creates a staff account (webmaster, tournament director, journalist, etc.) without setting a password. The invitee receives an email with a link to set their own password.";

        Response(201, "The account was created and an invite email was sent.",
            contentType: MediaTypeNames.Application.Json,
            example: new CreateUserResponse { UserId = "01JXXXXXXXXXXXXXXXXXXXXXXXXX" });

        Response(400, "The request was malformed.");
        Response(401, "The caller is not authenticated.");
        Response(403, "The caller does not hold the CreateUser permission.");
        Response(409, "An account with this email already exists.");
        Response(422, "Validation failed (invalid email, missing/unknown roles, Admin role requested).");
    }
}
