using System.Security.Claims;

using ErrorOr;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Neba.Api.Contracts.Security;
using Neba.Api.Database;
using Neba.Api.Messaging;
using Neba.Api.Security.Domain;

namespace Neba.Api.Security.UpdateUser;

internal sealed class UpdateUserCommandHandler(
    SecurityDbContext securityDbContext,
    ILogger<UpdateUserCommandHandler> logger)
    : ICommandHandler<UpdateUserCommand, Updated>
{
    public async Task<ErrorOr<Updated>> HandleAsync(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        if (command.UserId == command.CallerUserId)
        {
            return UpdateUserErrors.CannotEditOwnAccount;
        }

        var target = await securityDbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == command.UserId)
            .Select(u => new
            {
                IsAdmin = securityDbContext.UserRoles.Any(userRole => userRole.UserId == u.Id
                    && securityDbContext.Roles.Any(role => role.Id == userRole.RoleId && role.Name == Roles.Admin))
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (target is null)
        {
            return UpdateUserErrors.UserNotFound;
        }

        if (target.IsAdmin)
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

        if (requested.Value.Added.SetEquals(previous.Added) && requested.Value.Removed.SetEquals(previous.Removed))
        {
            return Result.Updated;
        }

        // One SaveChanges: the replace is all-or-nothing, so a failure can't leave a half-applied set.
        RemoveStale(requested.Value, stored);

        AddMissing(command.UserId, Permissions.ClaimType, requested.Value.Added, stored);
        AddMissing(command.UserId, Permissions.DeniedClaimType, requested.Value.Removed, stored);

        await securityDbContext.SaveChangesAsync(cancellationToken);

        var added = string.Join(", ", requested.Value.Added.Order(StringComparer.Ordinal));
        var removed = string.Join(", ", requested.Value.Removed.Order(StringComparer.Ordinal));

        logger.LogPermissionOverridesUpdated(command.UserId, command.CallerUserId, added, removed);

        return Result.Updated;
    }

    private void RemoveStale(UserPermissionOverrides requested, IEnumerable<IdentityUserClaim<Ulid>> stored)
    {
        var stale = stored.Where(claim =>
            !(claim.ClaimType == Permissions.ClaimType ? requested.Added : requested.Removed).Contains(claim.ClaimValue!));

        foreach (var claim in stale)
        {
            securityDbContext.UserClaims.Remove(claim);
        }
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

internal static partial class UpdateUserCommandHandlerLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Permission overrides for user {UserId} set by {CallerUserId}: added [{Added}], removed [{Removed}]")]
    public static partial void LogPermissionOverridesUpdated(
        this ILogger<UpdateUserCommandHandler> logger, Ulid userId, Ulid callerUserId, string added, string removed);
}