using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.Security.RefreshToken;

namespace Neba.Api.Security.RefreshToken;

internal sealed class RefreshTokenSummary : Summary<RefreshTokenEndpoint>
{
    public RefreshTokenSummary()
    {
        Summary = "Exchanges a refresh token for a new token pair.";
        Description = "Validates the refresh token (7-day window, hashed at rest) and issues a new access token and rotated refresh token. Always returns 401 for invalid or expired tokens.";

#pragma warning disable S1075
        Response(200, "Token refresh successful.",
            contentType: MediaTypeNames.Application.Json,
            example: new RefreshTokenResponse
            {
#pragma warning disable S6418
                AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
#pragma warning restore S6418
                RefreshToken = "xyz789...",
                ExpiresAt = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero),
                UserId = "01JXXXXXXXXXXXXXXXXXXXXXXXXX",
                Email = "admin@bowlneba.com",
            });
#pragma warning restore S1075

        Response(401, "Invalid, expired, or already-rotated refresh token.");
        Response(422, "Validation failed (missing or malformed UserId or RefreshToken).");
    }
}