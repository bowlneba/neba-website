using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Messaging;

namespace Neba.Api.Security.ListUsers;

internal sealed class ListUsersQueryHandler(SecurityDbContext securityDbContext)
    : IQueryHandler<ListUsersQuery, IReadOnlyCollection<UserSummaryDto>>
{
    public async Task<IReadOnlyCollection<UserSummaryDto>> HandleAsync(ListUsersQuery query, CancellationToken cancellationToken)
    {
        var users = await securityDbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .Select(user => new
            {
                user.Id,
                user.Email,
                user.EmailConfirmed,
                Roles = securityDbContext.UserRoles
                    .Where(userRole => userRole.UserId == user.Id)
                    .Join(securityDbContext.Roles, userRole => userRole.RoleId, role => role.Id, (userRole, role) => role.Name!)
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return [.. users.Select(user => new UserSummaryDto
        {
            UserId = user.Id,
            Email = user.Email!,
            EmailConfirmed = user.EmailConfirmed,
            Roles = user.Roles
        })];
    }
}