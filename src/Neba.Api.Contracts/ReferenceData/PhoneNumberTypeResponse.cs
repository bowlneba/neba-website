namespace Neba.Api.Contracts.ReferenceData;

/// <summary>
/// Represents a single phone number type (e.g. Home, Mobile, Work, Fax) as a display name / code pair, for populating phone-type dropdowns.
/// </summary>
public sealed record PhoneNumberTypeResponse
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