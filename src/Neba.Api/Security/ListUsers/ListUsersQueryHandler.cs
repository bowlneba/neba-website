using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Messaging;

namespace Neba.Api.Security.ListUsers;

internal sealed class ListUsersQueryHandler(SecurityDbContext securityDbContext)
    : IQueryHandler<ListUsersQuery, PagedResult<UserSummaryDto>>
{
    public async Task<PagedResult<UserSummaryDto>> HandleAsync(ListUsersQuery query, CancellationToken cancellationToken)
    {
        var baseQuery = securityDbContext.Users.AsNoTracking();

        var totalItems = await baseQuery.CountAsync(cancellationToken);

        var users = await baseQuery
            .OrderBy(user => user.Email)
            .ApplyPagination(query)
            .Select(user => new
            {
                user.Id,
                user.Email,
                user.EmailConfirmed,
                Roles = securityDbContext.UserRoles
                    .Where(userRole => userRole.UserId == user.Id)
                    .Join(securityDbContext.Roles, userRole => userRole.RoleId, role => role.Id, (_, role) => role.Name!)
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var items = users.Select(user => new UserSummaryDto
        {
            UserId = user.Id,
            Email = user.Email!,
            EmailConfirmed = user.EmailConfirmed,
            Roles = user.Roles
        });

        return new PagedResult<UserSummaryDto>([.. items], totalItems);
    }
}