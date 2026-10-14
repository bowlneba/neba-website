using Microsoft.EntityFrameworkCore;

using Neba.Api.Contracts.Security;
using Neba.Api.Database;
using Neba.Api.Security.Domain;

namespace Neba.Api.Security.Infrastructure.Authorization;

internal sealed class PermissionDenialReader(SecurityDbContext securityDbContext)
    : IPermissionDenialReader
{
    private static readonly string NormalizedAdminRole = Roles.Admin.ToUpperInvariant();

    /// <summary>
    /// Admin holds every permission by definition, so a stored denial never narrows it. Admin comes
    /// from the stored roles, like <see cref="PermissionResolver"/>, not from the JWT, so a
    /// promotion or demotion takes effect at once.
    /// </summary>
    public Task<bool> IsDeniedAsync(Ulid userId, Permissions permission, CancellationToken cancellationToken)
        => securityDbContext.UserClaims
            .AsNoTracking()
            .AnyAsync(
                claim => claim.UserId == userId
                    && claim.ClaimType == Permissions.DeniedClaimType
                    && claim.ClaimValue == permission.Value
                    && !securityDbContext.UserRoles.Any(userRole => userRole.UserId == userId
                        && securityDbContext.Roles.Any(role => role.Id == userRole.RoleId
                            && role.NormalizedName == NormalizedAdminRole)),
                cancellationToken);
}