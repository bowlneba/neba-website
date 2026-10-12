using Microsoft.EntityFrameworkCore;

using Neba.Api.Contracts.Security;
using Neba.Api.Database;
using Neba.Api.Messaging;

namespace Neba.Api.Security.ListRoles;

internal sealed class ListRolesQueryHandler(SecurityDbContext securityDbContext)
    : IQueryHandler<ListRolesQuery, IReadOnlyCollection<RoleDto>>
{
    public async Task<IReadOnlyCollection<RoleDto>> HandleAsync(ListRolesQuery query, CancellationToken cancellationToken)
    {
        var roles = await securityDbContext.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .Select(role => new { role.Id, role.Name })
            .ToListAsync(cancellationToken);

        var grants = await securityDbContext.RoleClaims
            .AsNoTracking()
            .Where(claim => claim.ClaimType == Permissions.ClaimType)
            .Select(claim => new { claim.RoleId, claim.ClaimValue })
            .ToListAsync(cancellationToken);

        var permissionsByRole = grants.ToLookup(grant => grant.RoleId, grant => grant.ClaimValue!);

        return [.. roles.Select(role => new RoleDto
        {
            Name = role.Name!,
            Permissions = [.. permissionsByRole[role.Id].Order(StringComparer.Ordinal)]
        })];
    }
}
