using Neba.Domain.Contact;

namespace Neba.Application.Contact;

/// <summary>
/// Data Transfer Object (DTO) for representing a phone number in the application layer.
/// </summary>
public sealed record PhoneNumberDto
{
    /// <summary>
    /// Gets the type of phone number (e.g. Home, Mobile, Work, Fax).
    /// </summary>
    public required PhoneNumberType PhoneNumberType { get; init; }

    /// <summary>
    /// Gets the ISO country calling code for the phone number (e.g. "1" for North America).
    /// </summary>
    public required string Number { get ; init; }
}