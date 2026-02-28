using System.Text.RegularExpressions;

using ErrorOr;

namespace Neba.Domain.Contact;

/// <summary>
/// Represents an email address value object in the domain.
/// Stored and displayed as-is — no normalization is applied.
/// </summary>
public sealed partial record EmailAddress
{
    /// <summary>
    /// Gets an empty <see cref="EmailAddress"/> instance with a default (empty) value.
    /// Useful as a sentinel or default value where a non-null <see cref="EmailAddress"/> is required.
    /// </summary>
    public static EmailAddress Empty
        => new();

    /// <summary>
    /// Gets the email address value exactly as provided — no normalization is applied.
    /// </summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>
    /// Creates an <see cref="EmailAddress"/> from the provided string.
    /// </summary>
    /// <param name="email">The email address string to validate and store.</param>
    /// <returns>
    /// An <see cref="ErrorOr{T}"/> containing a valid <see cref="EmailAddress"/> on success;
    /// otherwise an error describing why the input was invalid.
    /// </returns>
    public static ErrorOr<EmailAddress> Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return EmailAddressErrors.EmailAddressIsRequired;
        }

        if (!ValidEmailAddress().IsMatch(email))
        {
            return EmailAddressErrors.InvalidEmailAddress(email);
        }

        return new EmailAddress { Value = email };
    }

    /// <summary>
    /// Regex that matches a structurally valid email address.
    /// Requires non-whitespace characters before and after @, with a dot and at least
    /// two characters in the TLD portion of the domain.
    /// </summary>
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$")]
    private static partial Regex ValidEmailAddress();
}

internal static class EmailAddressErrors
{
    public static readonly Error EmailAddressIsRequired =
        Error.Validation(
            code: "EmailAddress.EmailAddressIsRequired",
            description: "Email address is required.");

    public static Error InvalidEmailAddress(string providedEmail)
        => Error.Validation(
            code: "EmailAddress.InvalidEmailAddress",
            description: "The provided value is not a valid email address.",
            metadata: new Dictionary<string, object>
            {
                { "InvalidEmailAddress", providedEmail }
            });
}