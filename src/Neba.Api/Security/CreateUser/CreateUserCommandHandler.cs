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
    WebsiteSettings websiteSettings)
        : ICommandHandler<CreateUserCommand, Ulid>
{
    public async Task<ErrorOr<Ulid>> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken)
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

        await userManager.AddToRolesAsync(user, command.Roles);

        if (command.Claims.Count > 0)
        {
            var claims = command.Claims.Select(c => new Claim(c.Type, c.Value));
            await userManager.AddClaimsAsync(user, claims);
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var inviteLink = $"{websiteSettings.BaseUrl}/account/set-password?userId={user.Id}&token={WebUtility.UrlEncode(token)}";

        await emailSender.SendAsync(new EmailMessage
        {
            To = user.Email,
            Subject = "You've been invited to BowlNEBA",
            HtmlBody = new InviteUserEmail(inviteLink).ToHtmlBody()
        }, cancellationToken);

        return user.Id;
    }
}