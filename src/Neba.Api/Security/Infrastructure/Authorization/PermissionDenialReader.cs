using Microsoft.EntityFrameworkCore;

using Neba.Api.Contracts.Security;
using Neba.Api.Database;

namespace Neba.Api.Security.Infrastructure.Authorization;

internal sealed class PermissionDenialReader(SecurityDbContext securityDbContext)
    : IPermissionDenialReader
{
    public Task<bool> IsDeniedAsync(Ulid userId, Permissions permission, CancellationToken cancellationToken)
        => securityDbContext.UserClaims
            .AsNoTracking()
            .AnyAsync(
                claim => claim.UserId == userId
                    && claim.ClaimType == Permissions.DeniedClaimType
                    && claim.ClaimValue == permission.Value,
                cancellationToken);
}