using MailKit.Security;

namespace Neba.Api.Email;

internal sealed class EmailSettings
{
    public const string SectionName = "EmailSettings";

    public string Host { get; set; } = "smtp.gmail.com";

    public int Port { get; set; } = 587;

    public string UserName { get; set; } = string.Empty;

    public string FromAddress { get; init; } = string.Empty;

    public string AppPassword { get; set; } = string.Empty;

    public string FromName { get; init; } = "BowlNEBA";

    public string ReplyToAddress { get; init; } = string.Empty;

    public string ReplyToName { get; init; } = "BowlNEBA Support";

    public SecureSocketOptions TlsMode { get; set; } = SecureSocketOptions.StartTls;
}