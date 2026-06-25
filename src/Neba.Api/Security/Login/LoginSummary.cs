using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.Security.Login;

namespace Neba.Api.Security.Login;

internal sealed class LoginSummary : Summary<LoginEndpoint>
{
    public LoginSummary()
    {
        Summary = "Authenticates a user and returns tokens.";
        Description = "Validates email and password. Returns a signed JWT access token (15 min) and an opaque refresh token (7 days). Always returns 401 for both wrong password and unknown email to avoid user enumeration.";

#pragma warning disable S1075
        Response(200, "Login successful.",
            contentType: MediaTypeNames.Application.Json,
            example: new LoginResponse
            {
#pragma warning disable S6418
                AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
#pragma warning restore S6418
                RefreshToken = "abc123...",
                ExpiresAt = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero),
                UserId = "01JXXXXXXXXXXXXXXXXXXXXXXXXX",
                Email = "admin@bowlneba.com",
            });
#pragma warning restore S1075

        Response(401, "Invalid email or password.");
        Response(422, "Validation failed (missing email, missing password, or invalid email format).");
    }
}
