using System.Text.RegularExpressions;

namespace Neba.Api.Compliance;

/// <summary>
/// Masks email addresses embedded in free-text sent to Discord — a new outbound channel that,
/// unlike <see cref="ILogger"/>, isn't wrapped by <see cref="RedactionConfiguration.AddRedaction"/>.
/// Exception/validation messages routinely quote an email address (e.g. "Bowler with email
/// x@example.com already exists"), and that text is forwarded to Discord verbatim by
/// <c>GlobalExceptionHandler</c>, <c>ResilientAuditDataProvider</c>, and
/// <c>DiscordJobFailureFilter</c>. This can't reuse the attribute-driven
/// <see cref="AuditPayloadScrubber"/>/<c>[PersonalData]</c> pipeline, both of which classify known
/// properties on a typed object graph or logger arguments — an exception message is unstructured
/// text with no property to attribute-tag.
/// </summary>
internal static partial class DiscordMessageRedactor
{
    public static string Redact(string text)
        => string.IsNullOrEmpty(text)
            ? text
            : EmailRegex().Replace(text, match => Mask(match.Value));

    private static string Mask(string value)
        => value.Length <= 1
            ? value
            : $"{value[0]}{new string('*', value.Length - 1)}";

    [GeneratedRegex(@"[a-zA-Z0-9.+_-]+@[a-zA-Z0-9-]+\.[a-zA-Z]{2,}")]
    private static partial Regex EmailRegex();
}