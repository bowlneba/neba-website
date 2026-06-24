using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.Security.Register;

namespace Neba.Api.Security.Register;

internal sealed class RegisterSummary : Summary<RegisterEndpoint>
{
    public RegisterSummary()
    {
        Summary = "Registers a new user account.";
        Description = "Creates a new user account with the given email and password. Day 1: admin-created accounts only. Email is auto-confirmed — the user can log in immediately.";

        Response(201, "The account was created successfully.",
            contentType: MediaTypeNames.Application.Json,
            example: new RegisterResponse { UserId = "01JXXXXXXXXXXXXXXXXXXXXXXXXX" });

        Response(409, "An account with this email already exists.");
        Response(422, "Validation failed (invalid email format, password too short, etc.).");
    }
}