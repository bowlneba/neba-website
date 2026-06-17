using Microsoft.AspNetCore.Identity;

using Neba.Api.Security.Domain;
using Neba.Api.Security.Emails;

namespace Neba.Api.Email;

internal sealed class IdentityEmailSenderAdapter(IEmailSender sender)
    : IEmailSender<ApplicationUser>
{
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
        => sender.SendAsync(new EmailMessage
        {
            To = email,
            Subject = "Confirm your BowlNEBA Account",
            HtmlBody = new ConfirmAccountEmail(confirmationLink).ToHtmlBody()
        }, CancellationToken.None);

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
        => sender.SendAsync(new EmailMessage
        {
            To = email,
            Subject = "Reset your BowlNEBA Password",
            HtmlBody = new ResetPasswordCodeEmail(resetCode).ToHtmlBody()
        }, CancellationToken.None);

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
        => sender.SendAsync(new EmailMessage
        {
            To = email,
            Subject = "Reset your BowlNEBA Password",
            HtmlBody = new ResetPasswordLinkEmail(resetLink).ToHtmlBody()
        }, CancellationToken.None);
}
