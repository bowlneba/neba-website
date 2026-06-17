namespace Neba.Api.Email;

internal sealed class EmailSettings
{
    public const string SectionName = "EmailSettings";

    public string Host { get; init; } = "smtp.gmail.com";

    public int Port { get; init; } = 587;

    public string UserName { get; init; } = string.Empty;

    public string FromAddress { get; init; } = string.Empty;

    public string AppPassword { get; init; } = string.Empty;

    public string FromName { get; init; } = "BowlNEBA";

    public string ReplyToAddress { get; init; } = string.Empty;

    public string ReplyToName { get; init; } = "BowlNEBA Support";
}