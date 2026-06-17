namespace Neba.Api.Email;

/// <summary>
/// Represents an email message to be sent, including recipient, subject, body, and optional reply-to address.
/// </summary>
public sealed record EmailMessage
{
    /// <summary>
    /// The recipient's email address. This is a required field and must be a valid email format.
    /// </summary>
    public required string To { get; init; }

    /// <summary>
    /// The subject of the email. This is a required field and should be concise and descriptive of the email's content.
    /// </summary>
    public required string Subject { get; init; }

    /// <summary>
    /// The HTML body of the email. This is a required field and should contain the main content of the email, formatted in HTML for rich text presentation.
    /// </summary>
    public required string HtmlBody { get; init; }

    /// <summary>
    /// An optional email address that recipients can reply to. If not provided, replies will go to the default sender address. This should be a valid email format if specified.
    /// </summary>
    public string? ReplyTo { get; init; }
}