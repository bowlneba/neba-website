using Microsoft.AspNetCore.Identity;

using Neba.Api.Security.Domain;

namespace Neba.Api.Email;

internal sealed class IdentityEmailSenderAdapter(IEmailSender sender)
    : IEmailSender<ApplicationUser>
{
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
        => sender.SendAsync(new EmailMessage
        {
            To = email,
            Subject = "Confirm your BowlNEBA Account",
            HtmlBody = $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>."
        }, CancellationToken.None);

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
        => sender.SendAsync(new EmailMessage
        {
            To = email,
            Subject = "Reset your BowlNEBA Password",
            HtmlBody = $"Your password reset code is: {resetCode}"
        }, CancellationToken.None);

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
        => sender.SendAsync(new EmailMessage
        {
            To = email,
            Subject = "Reset your BowlNEBA Password",
            HtmlBody = $"Reset your password by <a href='{resetLink}'>clicking here</a>."
        }, CancellationToken.None);
}