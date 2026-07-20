namespace Neba.Api.Contracts.Sponsors.CreateSponsor;

/// <summary>
/// A phone number entry supplied when creating a sponsor.
/// </summary>
public sealed record SponsorPhoneNumberInput
{
    /// <summary>
    /// The phone number type value (e.g. "H", "M", "W", "F" — see <c>PhoneNumberType</c>).
    /// </summary>
    public required string PhoneNumberType { get; init; }

    /// <summary>
    /// The phone number, which may include formatting characters.
    /// </summary>
    public required string PhoneNumber { get; init; }

    /// <summary>
    /// An optional extension.
    /// </summary>
    public string? Extension { get; init; }
}