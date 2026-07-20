using Neba.Api.Contracts.Sponsors.CreateSponsor;

namespace Neba.Website.Server.Sponsors;

/// <summary>
/// Request-contract mapping shared by the Create and Edit Sponsor forms — the pieces of
/// <c>BuildSponsorInput</c> that don't depend on which contract (<c>SponsorInput</c> vs
/// <c>EditSponsorInput</c>) the page ultimately builds.
/// </summary>
internal static class SponsorInputMapping
{
    public static Uri? ParseUri(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Uri.TryCreate(value, UriKind.Absolute, out var uri) 
            ? uri 
            : null;
    }

    public static IReadOnlyCollection<SponsorPhoneNumberInput> BuildPhoneNumberInputs(IEnumerable<TrackedPhoneNumber> phoneNumbers) =>
        [.. phoneNumbers
            .Where(p => !string.IsNullOrWhiteSpace(p.PhoneNumber))
            .Select(p => new SponsorPhoneNumberInput
            {
                PhoneNumberType = p.PhoneNumberType,
                PhoneNumber = p.PhoneNumber,
                Extension = string.IsNullOrWhiteSpace(p.Extension) ? null : p.Extension
            })];

    public static SponsorContactInput? BuildContactInput(
        string? contactName, string contactPhoneType, string? contactPhoneNumber, string? contactPhoneExtension, string? contactEmail)
    {
        var hasContact = !string.IsNullOrWhiteSpace(contactName)
            || !string.IsNullOrWhiteSpace(contactPhoneNumber)
            || !string.IsNullOrWhiteSpace(contactEmail);

        if (!hasContact)
        {
            return null;
        }

        return new SponsorContactInput
        {
            Name = contactName ?? string.Empty,
            PhoneNumberType = contactPhoneType,
            PhoneNumber = contactPhoneNumber ?? string.Empty,
            Extension = string.IsNullOrWhiteSpace(contactPhoneExtension) ? null : contactPhoneExtension,
            Email = contactEmail ?? string.Empty
        };
    }
}