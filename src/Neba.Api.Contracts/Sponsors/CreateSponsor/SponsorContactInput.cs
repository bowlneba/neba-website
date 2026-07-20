namespace Neba.Api.Contracts.Sponsors.CreateSponsor;

/// <summary>
/// Contact person details for a sponsor. All fields are required together — if any one is
/// supplied, all of Name, PhoneNumberType, PhoneNumber, and Email must be.
/// </summary>
public sealed record SponsorContactInput
{
    /// <summary>
    /// The contact person's name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The contact person's phone number type value (see <c>PhoneNumberType</c>).
    /// </summary>
    public required string PhoneNumberType { get; init; }

    /// <summary>
    /// The contact person's phone number, which may include formatting characters.
    /// </summary>
    public required string PhoneNumber { get; init; }

    /// <summary>
    /// An optional extension for the contact person's phone number.
    /// </summary>
    public string? Extension { get; init; }

    /// <summary>
    /// The contact person's email address.
    /// </summary>
    public required string Email { get; init; }
}
