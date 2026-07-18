using Neba.Api.Contracts.Contact;

namespace Neba.Api.Contracts.Sponsors;

/// <summary>
/// Represents a sponsor's internal contact person. Only returned to callers with sponsor-management permission.
/// </summary>
public sealed record SponsorContactResponse
{
    /// <summary>
    /// The contact person's name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The contact person's phone number.
    /// </summary>
    public required PhoneNumberResponse Phone { get; init; }

    /// <summary>
    /// The contact person's email address.
    /// </summary>
    public required string Email { get; init; }
}
