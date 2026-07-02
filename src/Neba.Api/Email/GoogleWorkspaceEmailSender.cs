using MailKit.Net.Smtp;

using MimeKit;

namespace Neba.Api.Email;

internal sealed class GoogleWorkspaceEmailSender(
    EmailSettings emailSettings,
    ILogger<GoogleWorkspaceEmailSender> logger)
        : IEmailSender
{
    private readonly EmailSettings _settings = emailSettings;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        using var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        mimeMessage.To.Add(MailboxAddress.Parse(message.To));
        mimeMessage.Subject = message.Subject;

        var replyToAddress = message.ReplyTo ?? _settings.ReplyToAddress;
        if (!string.IsNullOrEmpty(replyToAddress))
        {
            mimeMessage.ReplyTo.Add(new MailboxAddress(_settings.ReplyToName, replyToAddress));
        }

        mimeMessage.Body = new BodyBuilder
        {
            HtmlBody = message.HtmlBody
        }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port, _settings.TlsMode, cancellationToken);
        if (!string.IsNullOrEmpty(_settings.UserName))
            await client.AuthenticateAsync(_settings.UserName, _settings.AppPassword, cancellationToken);
        await client.SendAsync(mimeMessage, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogEmailSent(MaskEmail(message.To), message.Subject);
        }
    }

    // Masks the local part so recipient addresses never reach log storage in clear text.
    private static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@', StringComparison.Ordinal);
        return atIndex <= 0
            ? "***"
            : $"{email[0]}***{email[atIndex..]}";
    }
}

internal static partial class GoogleWorkspaceEmailSenderLogMessages
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Email sent to {ToAddress}: {Subject}")]
    public static partial void LogEmailSent(this ILogger<GoogleWorkspaceEmailSender> logger, string toAddress, string subject);
}