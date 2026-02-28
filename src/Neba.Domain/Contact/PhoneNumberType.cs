using Ardalis.SmartEnum;

namespace Neba.Domain.Contact;

/// <summary>
/// Represents the type of a phone number (e.g. Home, Mobile, Work, Fax) in the domain.
/// </summary>
public sealed class PhoneNumberType
    : SmartEnum<PhoneNumberType, string>
{
    /// <summary>
    /// Represents a home phone number type.
    /// </summary>
    public static readonly PhoneNumberType Home = new(nameof(Home), "H");

    /// <summary>
    /// Represents a mobile phone number type.
    /// </summary>
    public static readonly PhoneNumberType Mobile = new(nameof(Mobile), "M");

    /// <summary>
    /// Represents a work phone number type.
    /// </summary>
    public static readonly PhoneNumberType Work = new(nameof(Work), "W");

    /// <summary>
    /// Represents a fax phone number type.
    /// </summary>
    public static readonly PhoneNumberType Fax = new(nameof(Fax), "F");

    private PhoneNumberType(string name, string value)
        : base(name, value)
    { }
}