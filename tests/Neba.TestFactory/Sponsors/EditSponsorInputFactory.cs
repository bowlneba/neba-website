using Neba.Api.Contracts.Sponsors.CreateSponsor;
using Neba.Api.Contracts.Sponsors.EditSponsor;

namespace Neba.TestFactory.Sponsors;

public static class EditSponsorInputFactory
{
#pragma warning disable S107
    public static EditSponsorInput Create(
        string? name = null,
        bool? isCurrentSponsor = null,
        int? priority = null,
        string? tier = null,
        string? category = null,
        SponsorLogoInput? logo = null,
        Uri? websiteUrl = null,
        string? tagPhrase = null,
        string? description = null,
        string? liveReadText = null,
        string? promotionalNotes = null,
        Uri? facebookUrl = null,
        Uri? instagramUrl = null,
        string? businessStreet = null,
        string? businessUnit = null,
        string? businessCity = null,
        string? businessState = null,
        string? businessPostalCode = null,
        string? businessEmailAddress = null,
        IReadOnlyCollection<SponsorPhoneNumberInput>? phoneNumbers = null,
        SponsorContactInput? contact = null)
            => new()
            {
                Name = name ?? SponsorFactory.ValidName,
                IsCurrentSponsor = isCurrentSponsor ?? SponsorFactory.ValidIsCurrentSponsor,
                Priority = priority ?? SponsorFactory.ValidPriority,
                Tier = tier ?? SponsorFactory.ValidTier.Name,
                Category = category ?? SponsorFactory.ValidCategory.Name,
                Logo = logo,
                WebsiteUrl = websiteUrl,
                TagPhrase = tagPhrase,
                Description = description,
                LiveReadText = liveReadText,
                PromotionalNotes = promotionalNotes,
                FacebookUrl = facebookUrl,
                InstagramUrl = instagramUrl,
                BusinessStreet = businessStreet,
                BusinessUnit = businessUnit,
                BusinessCity = businessCity,
                BusinessState = businessState,
                BusinessPostalCode = businessPostalCode,
                BusinessEmailAddress = businessEmailAddress,
                PhoneNumbers = phoneNumbers ?? [],
                Contact = contact
            };
#pragma warning restore S107
}