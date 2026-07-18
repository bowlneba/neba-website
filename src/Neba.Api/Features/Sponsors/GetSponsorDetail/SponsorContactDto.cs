using Neba.Api.Contacts;

namespace Neba.Api.Features.Sponsors.GetSponsorDetail;

/// <summary>
/// Data transfer object representing a sponsor's internal contact person. Only populated for
/// callers with sponsor-management permission.
/// </summary>
public sealed record SponsorContactDto
{
    /// <summary>
    /// The contact person's name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The contact person's phone number.
    /// </summary>
    public required PhoneNumberDto Phone { get; init; }

    /// <summary>
    /// The contact person's email address.
    /// </summary>
    public required string Email { get; init; }
}
