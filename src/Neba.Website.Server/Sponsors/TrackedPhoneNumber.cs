namespace Neba.Website.Server.Sponsors;

/// <summary>
/// Editable phone-number row shared by the Create and Edit Sponsor forms and
/// <see cref="SponsorPhoneNumbersEditor"/>.
/// </summary>
public sealed class TrackedPhoneNumber
{
    /// <summary>The phone number type code (e.g. "H", "M").</summary>
    public string PhoneNumberType { get; set; } = "H";

    /// <summary>The phone number.</summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>The optional extension.</summary>
    public string? Extension { get; set; }
}
