using System.Diagnostics.CodeAnalysis;

using MailKit.Net.Smtp;

using MimeKit;

using Neba.Api.Compliance;
using Neba.Api.Discord;

namespace Neba.Api.Email;

internal sealed class GoogleWorkspaceEmailSender(
    EmailSettings emailSettings,
    IDiscordNotifier discordNotifier,
    ILogger<GoogleWorkspaceEmailSender> logger)
        : IEmailSender
{
    private readonly EmailSettings _settings = emailSettings;
    private readonly IDiscordNotifier _discordNotifier = discordNotifier;

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "SMTP failures must post to Discord before the job still fails visibly; every failure mode gets identical treatment.")]
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

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port, _settings.TlsMode, cancellationToken);
            if (!string.IsNullOrEmpty(_settings.UserName))
                await client.AuthenticateAsync(_settings.UserName, _settings.AppPassword, cancellationToken);
            await client.SendAsync(mimeMessage, cancellationToken);
            await client.DisconnectAsync(quit: true, cancellationToken);

            logger.LogEmailSent(message.To, message.Subject);
        }
        catch (Exception ex)
        {
            var alert = new DiscordAlert(
                DiscordAlertSeverity.Critical,
                "Email delivery failed",
                ex.Message,
                new Dictionary<string, string>
                {
                    ["Recipient"] = message.To,
                    ["Subject"] = message.Subject
                });

            await _discordNotifier.NotifyAsync(alert, cancellationToken);
        }
    }
}

internal static partial class GoogleWorkspaceEmailSenderLogMessages
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Email sent to {ToAddress}: {Subject}")]
    public static partial void LogEmailSent(this ILogger<GoogleWorkspaceEmailSender> logger, [PersonalData] string toAddress, string subject);
}