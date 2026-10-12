using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Contracts.Security;
using Neba.Api.Database;
using Neba.Api.Messaging;
using Neba.Api.Security.Domain;

namespace Neba.Api.Security.GetUser;

internal sealed class GetUserQueryHandler(SecurityDbContext securityDbContext)
    : IQueryHandler<GetUserQuery, ErrorOr<UserDetailDto>>
{
    public async Task<ErrorOr<UserDetailDto>> HandleAsync(GetUserQuery query, CancellationToken cancellationToken)
    {
        var user = await securityDbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == query.UserId)
            .Select(u => new { u.Id, u.Email, u.EmailConfirmed })
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return GetUserErrors.UserNotFound;
        }

        var roles = await securityDbContext.UserRoles
            .Where(userRole => userRole.UserId == query.UserId)
            .Join(securityDbContext.Roles, userRole => userRole.RoleId, role => role.Id, (_, role) => role.Name!)
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);

        var claims = await securityDbContext.UserClaims
            .AsNoTracking()
            .Where(c => c.UserId == query.UserId
                && (c.ClaimType == Permissions.ClaimType || c.ClaimType == Permissions.DeniedClaimType))
            .ToListAsync(cancellationToken);

        var overrides = UserPermissionOverrides.FromStoredClaims(claims.Select(c => c.ToClaim()));

        return new UserDetailDto
        {
            UserId = user.Id,
            Email = user.Email!,
            EmailConfirmed = user.EmailConfirmed,
            Roles = roles,
            AddedPermissions = [.. overrides.Added.Order(StringComparer.Ordinal)],
            RemovedPermissions = [.. overrides.Removed.Order(StringComparer.Ordinal)]
        };
    }
}
