using System.Net;

using Microsoft.AspNetCore.Identity;

using Neba.Api.Contracts.Security;
using Neba.Api.Email;
using Neba.Api.Security.Domain;
using Neba.Api.Security.Emails;

namespace Neba.Api.Security.Password.ForgotPassword;

internal sealed class SendPasswordResetEmailJob(
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    WebsiteSettings websiteSettings)
{
    // userId is a string so Hangfire stores it without a custom serializer.
    public async Task SendAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);

        if (user?.Email is null)
        {
            return;
        }

        var token = await userManager.GenerateUserTokenAsync(
            user, ResetTokenProvider.ProviderName, UserManager<ApplicationUser>.ResetPasswordTokenPurpose);
        var resetLink = $"{websiteSettings.BaseUrl}/account/set-password"
            + $"?userId={user.Id}&token={WebUtility.UrlEncode(token)}&reason={PasswordTokenPurpose.Reset}";

        await emailSender.SendAsync(new EmailMessage
        {
            To = user.Email,
            Subject = "Reset your BowlNEBA password",
            HtmlBody = new ResetPasswordLinkEmail(resetLink).ToHtmlBody()
        }, cancellationToken);
    }
}