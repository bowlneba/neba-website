using System.Net;
using System.Security.Claims;

using ErrorOr;

using Microsoft.AspNetCore.Identity;

using Neba.Api.Contracts.Security;
using Neba.Api.Email;
using Neba.Api.Messaging;
using Neba.Api.Security.Domain;
using Neba.Api.Security.Emails;

namespace Neba.Api.Security.CreateUser;

internal sealed class CreateUserCommandHandler(
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    WebsiteSettings websiteSettings,
    ILogger<CreateUserCommandHandler> logger)
        : ICommandHandler<CreateUserCommand, CreateUserResult>
{
    public async Task<ErrorOr<CreateUserResult>> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        // Build and guard before creating anything: a rejected request must not leave an invited account behind.
        var overrides = UserPermissionOverrides.Create(command.AddedPermissions, command.RemovedPermissions);

        if (overrides.IsError)
        {
            return overrides.Errors;
        }

        var notHeld = overrides.Value.GainsOver(UserPermissionOverrides.None)
            .Where(permission => !command.CallerPermissions.Contains(permission))
            .Order(StringComparer.Ordinal)
            .ToList();

        if (notHeld.Count > 0)
        {
            return CreateUserErrors.PermissionNotHeld(notHeld);
        }

        var user = new ApplicationUser
        {
            Id = Ulid.NewUlid(),
            UserName = command.Email,
            Email = command.Email,
            PhoneNumber = command.PhoneNumber,
            UsbcId = command.UsbcId,
            EmailConfirmed = false
        };

        var createResult = await userManager.CreateAsync(user);

        if (!createResult.Succeeded)
        {
            var isDuplicate = createResult.Errors.Any(error => error.Code is "DuplicateEmail" or "DuplicateUserName");

            return isDuplicate
                ? CreateUserErrors.DuplicateEmail
                : createResult.Errors
                    .Select(error => Error.Validation($"CreateUser.{error.Code}", error.Description))
                    .ToList();
        }

        var addToRolesResult = await userManager.AddToRolesAsync(user, command.Roles);

        if (!addToRolesResult.Succeeded)
        {
            var errors = string.Join("; ", addToRolesResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
            logger.LogRoleAssignmentFailed(user.Id, string.Join(", ", command.Roles), errors);
        }

        var rolesAssigned = addToRolesResult.Succeeded;

        var overridesApplied = true;

        if (overrides.Value.Added.Count > 0 || overrides.Value.Removed.Count > 0)
        {
            var claims = overrides.Value.Added.Select(value => new Claim(Permissions.ClaimType, value))
                .Concat(overrides.Value.Removed.Select(value => new Claim(Permissions.DeniedClaimType, value)));

            var addClaimsResult = await userManager.AddClaimsAsync(user, claims);

            if (!addClaimsResult.Succeeded)
            {
                overridesApplied = false;
                var errors = string.Join("; ", addClaimsResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                logger.LogClaimAssignmentFailed(user.Id, errors);
            }
        }

        var token = await userManager.GenerateUserTokenAsync(
            user, InviteTokenProvider.ProviderName, UserManager<ApplicationUser>.ResetPasswordTokenPurpose);
        var inviteLink = $"{websiteSettings.BaseUrl}/account/set-password"
            + $"?userId={user.Id}&token={WebUtility.UrlEncode(token)}&reason={PasswordTokenPurpose.Invite}";

        await emailSender.SendAsync(new EmailMessage
        {
            To = user.Email,
            Subject = "You've been invited to BowlNEBA",
            HtmlBody = new InviteUserEmail(inviteLink).ToHtmlBody()
        }, cancellationToken);

        return new CreateUserResult(user.Id, rolesAssigned, overridesApplied);
    }
}

internal static partial class CreateUserCommandHandlerLogMessages
{
    /// <summary>
    /// The user account is already created and the invite email still goes out even when role
    /// assignment fails — this is the only signal an admin gets that the requested roles weren't
    /// actually granted, so it must never be silently swallowed.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to assign role(s) '{Roles}' to newly created user {UserId}. Errors: {Errors}")]
    public static partial void LogRoleAssignmentFailed(
        this ILogger<CreateUserCommandHandler> logger, Ulid userId, string roles, string errors);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to store permission override(s) for newly created user {UserId}. Errors: {Errors}")]
    public static partial void LogClaimAssignmentFailed(
        this ILogger<CreateUserCommandHandler> logger, Ulid userId, string errors);
}