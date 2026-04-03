using Neba.Domain.Contact;

namespace Neba.Domain.Sponsors;

/// <summary>
/// Represents the contact information for a sponsor.
/// </summary>
public sealed record ContactInfo
{
    /// <summary>
    /// The name of the contact person.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The phone number of the contact person.
    /// </summary>
    public required PhoneNumber Phone { get; init; }

    /// <summary>
    /// The email address of the contact person.
    /// </summary>
    public required EmailAddress Email {get; init; }
}