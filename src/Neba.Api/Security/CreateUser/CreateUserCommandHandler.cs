using System.Net;
using System.Security.Claims;

using ErrorOr;

using Microsoft.AspNetCore.Identity;

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

        // Validated to only carry Permissions.ClaimType with a known permission value (see
        // CreateUserRequestValidator), but authorization never reads it back: JWTs only carry
        // permissions resolved from role membership (PermissionResolver), not from a user's own
        // stored claims. This lets an admin pre-stage a claim for a future feature without it
        // granting anything today.
        if (command.Claims.Count > 0)
        {
            var claims = command.Claims.Select(c => new Claim(c.Type, c.Value));
            var addClaimsResult = await userManager.AddClaimsAsync(user, claims);

            if (!addClaimsResult.Succeeded)
            {
                var errors = string.Join("; ", addClaimsResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                logger.LogClaimAssignmentFailed(user.Id, errors);
            }
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var inviteLink = $"{websiteSettings.BaseUrl}/account/set-password?userId={user.Id}&token={WebUtility.UrlEncode(token)}";

        await emailSender.SendAsync(new EmailMessage
        {
            To = user.Email,
            Subject = "You've been invited to BowlNEBA",
            HtmlBody = new InviteUserEmail(inviteLink).ToHtmlBody()
        }, cancellationToken);

        return new CreateUserResult(user.Id, rolesAssigned);
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
        Message = "Failed to assign claim(s) to newly created user {UserId}. Errors: {Errors}")]
    public static partial void LogClaimAssignmentFailed(
        this ILogger<CreateUserCommandHandler> logger, Ulid userId, string errors);
}