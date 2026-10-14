using System.Security.Claims;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Contracts.Security;
using Neba.Api.Database;
using Neba.Api.Security.Domain;

namespace Neba.Api.Security.Infrastructure.Authorization;

internal sealed class PermissionDenialReader(SecurityDbContext securityDbContext)
    : IPermissionDenialReader
{
    public Task<bool> IsDeniedAsync(ClaimsPrincipal caller, Permissions permission, CancellationToken cancellationToken)
    {
        // Admin holds every permission by definition; a stored denial never narrows it.
        if (caller.IsInRole(Roles.Admin))
        {
            return Task.FromResult(false);
        }

        return caller.CallerUserId is not { } userId
            ? Task.FromResult(false)
            : securityDbContext.UserClaims
            .AsNoTracking()
            .AnyAsync(
                claim => claim.UserId == userId
                    && claim.ClaimType == Permissions.DeniedClaimType
                    && claim.ClaimValue == permission.Value,
                cancellationToken);
    }
}