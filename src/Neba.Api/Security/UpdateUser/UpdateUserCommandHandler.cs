using System.Security.Claims;

using ErrorOr;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Neba.Api.Contracts.Security;
using Neba.Api.Database;
using Neba.Api.Messaging;
using Neba.Api.Security.Domain;

namespace Neba.Api.Security.UpdateUser;

internal sealed class UpdateUserCommandHandler(SecurityDbContext securityDbContext)
    : ICommandHandler<UpdateUserCommand, Updated>
{
    public async Task<ErrorOr<Updated>> HandleAsync(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        if (command.UserId == command.CallerUserId)
        {
            return UpdateUserErrors.CannotEditOwnAccount;
        }

        if (!await securityDbContext.Users.AnyAsync(u => u.Id == command.UserId, cancellationToken))
        {
            return UpdateUserErrors.UserNotFound;
        }

        var targetIsAdmin = await securityDbContext.UserRoles
            .Where(userRole => userRole.UserId == command.UserId)
            .Join(securityDbContext.Roles, userRole => userRole.RoleId, role => role.Id, (_, role) => role.Name)
            .AnyAsync(name => name == Roles.Admin, cancellationToken);

        if (targetIsAdmin)
        {
            return UpdateUserErrors.AdminOverridesNotAllowed;
        }

        var requested = UserPermissionOverrides.Create(command.AddedPermissions, command.RemovedPermissions);

        if (requested.IsError)
        {
            return requested.Errors;
        }

        var stored = await securityDbContext.UserClaims
            .Where(c => c.UserId == command.UserId
                && (c.ClaimType == Permissions.ClaimType || c.ClaimType == Permissions.DeniedClaimType))
            .ToListAsync(cancellationToken);

        var previous = UserPermissionOverrides.FromStoredClaims(stored.Select(c => c.ToClaim()));

        var notHeld = requested.Value.GainsOver(previous)
            .Where(permission => !command.CallerPermissions.Contains(permission))
            .Order(StringComparer.Ordinal)
            .ToList();

        if (notHeld.Count > 0)
        {
            return UpdateUserErrors.PermissionNotHeld(notHeld);
        }

        // One SaveChanges: the replace is all-or-nothing, so a failure can't leave a half-applied set.
        foreach (var claim in stored)
        {
            var wanted = claim.ClaimType == Permissions.ClaimType ? requested.Value.Added : requested.Value.Removed;

            if (!wanted.Contains(claim.ClaimValue!))
            {
                securityDbContext.UserClaims.Remove(claim);
            }
        }

        AddMissing(command.UserId, Permissions.ClaimType, requested.Value.Added, stored);
        AddMissing(command.UserId, Permissions.DeniedClaimType, requested.Value.Removed, stored);

        await securityDbContext.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }

    private void AddMissing(
        Ulid userId, string claimType, IReadOnlySet<string> wanted, IReadOnlyCollection<IdentityUserClaim<Ulid>> stored)
    {
        var missing = wanted.Where(value => !stored.Any(c => c.ClaimType == claimType && c.ClaimValue == value));

        foreach (var value in missing)
        {
            securityDbContext.UserClaims.Add(
                new IdentityUserClaim<Ulid> { UserId = userId, ClaimType = claimType, ClaimValue = value });
        }
    }
}