using Neba.Api.Email;
using Neba.Api.Security.Domain;
using Neba.Api.Security.Emails;

namespace Neba.Api.Security.Password;

/// <summary>
/// Sends the "your password was changed" notice. Best effort: the password change already happened.
/// </summary>
internal sealed class PasswordChangedNotifier(
    IEmailSender emailSender,
    WebsiteSettings websiteSettings,
    ILogger<PasswordChangedNotifier> logger)
{
    public async Task NotifyAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        try
        {
            await emailSender.SendAsync(new EmailMessage
            {
                To = user.Email!,
                Subject = "Your BowlNEBA password was changed",
                HtmlBody = new PasswordChangedEmail(websiteSettings.BaseUrl).ToHtmlBody()
            }, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogPasswordChangedNoticeFailed(exception, user.Id);
        }
    }
}

internal static partial class PasswordChangedNotifierLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to send the password-changed notice to user {UserId}. The password change itself succeeded.")]
    public static partial void LogPasswordChangedNoticeFailed(
        this ILogger<PasswordChangedNotifier> logger, Exception exception, Ulid userId);
}