namespace Neba.Api.ReferenceData.ListPhoneNumberTypes;

/// <summary>
/// Represents a single phone number type (e.g. Home, Mobile, Work, Fax) as a display name / code pair.
/// </summary>
public sealed record PhoneNumberTypeDto
{
    /// <summary>
    /// The phone number type's display name (e.g. "Mobile").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The phone number type's short code (e.g. "M").
    /// </summary>
    public required string Code { get; init; }
}
