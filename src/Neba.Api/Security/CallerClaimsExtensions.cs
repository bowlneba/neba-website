using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Neba.Api.Contracts.Security;

namespace Neba.Api.Security;

internal static class CallerClaimsExtensions
{
    extension(ClaimsPrincipal principal)
    {
        /// <summary>The caller's user ID from the JWT, or null when missing or not a ULID.</summary>
        public Ulid? CallerUserId
            => Ulid.TryParse(
                principal.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? principal.FindFirstValue(ClaimTypes.NameIdentifier),
                out var id)
                ? id
                : null;

        /// <summary>The permission values on the caller's JWT.</summary>
        public IReadOnlyCollection<string> CallerPermissions
            => [.. principal.FindAll(Permissions.ClaimType).Select(claim => claim.Value)];
    }
}