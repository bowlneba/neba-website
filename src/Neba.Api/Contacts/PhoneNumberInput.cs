using Neba.Api.Contracts.Contact;

namespace Neba.Api.Contacts;

/// <summary>
/// Raw phone number entry supplied to a command, prior to <c>PhoneNumber.CreateNorthAmerican</c> validation.
/// </summary>
public sealed record PhoneNumberInput
{
    /// <summary>
    /// The type of phone number (e.g., mobile, home, work).
    /// </summary>
    public required PhoneNumberType Type { get; init; }

    /// <summary>
    /// The raw phone number string.
    /// </summary>
    public required string Number { get; init; }

    /// <summary>
    /// The phone number extension, if any.
    /// </summary>
    public string? Extension { get; init; }
}