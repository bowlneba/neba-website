using Neba.Api.Contacts;
using Neba.Api.Contacts.Domain;
using Neba.Api.Contracts.Contact;
using Neba.Api.Contracts.Sponsors.CreateSponsor;
using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Storage.Domain;

namespace Neba.Api.Features.Sponsors;

/// <summary>
/// Maps the request-contract sub-shapes shared by <c>SponsorInput</c> and <c>EditSponsorInput</c>
/// (logo, business state, phone numbers, contact) onto their command-layer equivalents. Shared by
/// <see cref="CreateSponsor.CreateSponsorEndpoint"/> and <see cref="EditSponsor.EditSponsorEndpoint"/>.
/// </summary>
internal static class SponsorCommandMapper
{
    public static StoredFile? MapLogo(SponsorLogoInput? logo) =>
        logo is null
            ? null
            : new StoredFile
            {
                Container = logo.Container,
                Path = logo.Path,
                ContentType = logo.ContentType,
                SizeInBytes = logo.SizeInBytes
            };

    public static UsState? MapBusinessState(string? businessState) =>
        string.IsNullOrWhiteSpace(businessState) ? null : UsState.FromValue(businessState);

    public static IReadOnlyCollection<PhoneNumberInput> MapPhoneNumbers(IReadOnlyCollection<SponsorPhoneNumberInput> phoneNumbers) =>
        [.. phoneNumbers.Select(p => new PhoneNumberInput
        {
            Type = PhoneNumberType.FromValue(p.PhoneNumberType),
            Number = p.PhoneNumber,
            Extension = p.Extension
        })];

    public static (string? Name, PhoneNumberType? PhoneType, string? PhoneNumber, string? PhoneExtension, string? Email) MapContact(
        SponsorContactInput? contact) => (
            contact?.Name,
            contact is null ? null : PhoneNumberType.FromValue(contact.PhoneNumberType),
            contact?.PhoneNumber,
            contact?.Extension,
            contact?.Email);
}