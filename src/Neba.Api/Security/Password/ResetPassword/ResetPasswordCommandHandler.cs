using System.Net;

using ErrorOr;

using Microsoft.AspNetCore.Identity;

using Neba.Api.Email;
using Neba.Api.Messaging;
using Neba.Api.Security.Domain;
using Neba.Api.Security.Emails;

namespace Neba.Api.Security.Password.ResetPassword;

internal sealed class ResetPasswordCommandHandler(
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    WebsiteSettings websiteSettings)
    : ICommandHandler<ResetPasswordCommand>
{
    public async Task<ErrorOr<Success>> HandleAsync(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(command.UserId.ToString());

        if (user is null)
        {
            return ResetPasswordErrors.UserNotFound;
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetLink = $"{websiteSettings.BaseUrl}/account/set-password?userId={user.Id}&token={WebUtility.UrlEncode(token)}";

        await emailSender.SendAsync(new EmailMessage
        {
            To = user.Email!,
            Subject = "Your BowlNEBA password has been reset",
            HtmlBody = new AdminResetPasswordLinkEmail(resetLink).ToHtmlBody()
        }, cancellationToken);

        return Result.Success;
    }
}