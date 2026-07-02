using ErrorOr;

using Microsoft.AspNetCore.Identity;

using Neba.Api.Messaging;
using Neba.Api.Security.Domain;
using Neba.Api.Security.Infrastructure.Authorization;

namespace Neba.Api.Security.GetCurrentUser;

internal sealed class GetCurrentUserQueryHandler(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    : IQueryHandler<GetCurrentUserQuery, ErrorOr<UserDto>>
{
    public async Task<ErrorOr<UserDto>> HandleAsync(GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(query.UserId.ToString());

        if (user is null)
        {
            return GetCurrentUserErrors.UserNotFound;
        }

        var roles = await userManager.GetRolesAsync(user);
        var permissions = await PermissionResolver.ResolveAsync(roleManager, roles);

        return new UserDto
        {
            UserId = user.Id,
            Email = user.Email!,
            Roles = [.. roles],
            Permissions = permissions,
            UsbcId = user.UsbcId
        };
    }
}