using Neba.Api.Features.Sponsors;
using Neba.Api.Features.Sponsors.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Contact;
using Neba.TestFactory.Sponsors;
using Neba.TestFactory.Storage;

namespace Neba.Api.Tests.Features.Sponsors.Domain;

[UnitTest]
[Component("Sponsors.Sponsor")]
public sealed class SponsorTests
{
    // ── Create ─────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Create should return an error when name is null")]
    public void Create_ShouldReturnError_WhenNameIsNull()
    {
#nullable disable
        // Arrange & Act
        var result = Sponsor.Create(
            name: null,
            isCurrentSponsor: SponsorFactory.ValidIsCurrentSponsor,
            priority: SponsorFactory.ValidPriority,
            tier: SponsorFactory.ValidTier,
            category: SponsorFactory.ValidCategory);
#nullable enable

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SponsorErrors.NameRequired);
    }

    [Theory(DisplayName = "Create should return an error when name is empty or whitespace")]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldReturnError_WhenNameIsEmptyOrWhitespace(string name)
    {
        // Arrange & Act
        var result = Sponsor.Create(
            name: name,
            isCurrentSponsor: SponsorFactory.ValidIsCurrentSponsor,
            priority: SponsorFactory.ValidPriority,
            tier: SponsorFactory.ValidTier,
            category: SponsorFactory.ValidCategory);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SponsorErrors.NameRequired);
    }

    [Fact(DisplayName = "Create should return an error when the normalized slug has no alphanumeric characters")]
    public void Create_ShouldReturnError_WhenNormalizedSlugHasNoAlphanumericCharacters()
    {
        // Arrange & Act
        var result = Sponsor.Create(
            name: SponsorFactory.ValidName,
            isCurrentSponsor: SponsorFactory.ValidIsCurrentSponsor,
            priority: SponsorFactory.ValidPriority,
            tier: SponsorFactory.ValidTier,
            category: SponsorFactory.ValidCategory,
            slug: "---!!!---");

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SponsorErrors.SlugInvalid);
    }

    [Fact(DisplayName = "Create should return an error when the normalized slug is the reserved value 'new'")]
    public void Create_ShouldReturnError_WhenNormalizedSlugIsReserved()
    {
        // Arrange & Act
        var result = Sponsor.Create(
            name: SponsorFactory.ValidName,
            isCurrentSponsor: SponsorFactory.ValidIsCurrentSponsor,
            priority: SponsorFactory.ValidPriority,
            tier: SponsorFactory.ValidTier,
            category: SponsorFactory.ValidCategory,
            slug: "New");

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SponsorErrors.SlugReserved);
    }

    [Fact(DisplayName = "Create should return an error when the name normalizes to the reserved slug and no explicit slug is given")]
    public void Create_ShouldReturnError_WhenNameNormalizesToReservedSlug()
    {
        // Arrange & Act
        var result = Sponsor.Create(
            name: "New",
            isCurrentSponsor: SponsorFactory.ValidIsCurrentSponsor,
            priority: SponsorFactory.ValidPriority,
            tier: SponsorFactory.ValidTier,
            category: SponsorFactory.ValidCategory);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SponsorErrors.SlugReserved);
    }

    [Fact(DisplayName = "Create should generate the slug from name when slug is null")]
    public void Create_ShouldGenerateSlugFromName_WhenSlugIsNull()
    {
        // Arrange & Act
        var result = Sponsor.Create(
            name: "Joe's Sponsorship Company",
            isCurrentSponsor: SponsorFactory.ValidIsCurrentSponsor,
            priority: SponsorFactory.ValidPriority,
            tier: SponsorFactory.ValidTier,
            category: SponsorFactory.ValidCategory,
            slug: null);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Slug.ShouldBe("joe-s-sponsorship-company");
    }

    [Fact(DisplayName = "Create should generate the slug from name when slug is empty")]
    public void Create_ShouldGenerateSlugFromName_WhenSlugIsEmpty()
    {
        // Arrange & Act
        var result = Sponsor.Create(
            name: "Joe's Sponsorship Company",
            isCurrentSponsor: SponsorFactory.ValidIsCurrentSponsor,
            priority: SponsorFactory.ValidPriority,
            tier: SponsorFactory.ValidTier,
            category: SponsorFactory.ValidCategory,
            slug: string.Empty);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Slug.ShouldBe("joe-s-sponsorship-company");
    }

    [Fact(DisplayName = "Create should return an error when slug is whitespace-only, since it is not treated as empty")]
    public void Create_ShouldReturnError_WhenSlugIsWhitespaceOnly()
    {
        // Arrange & Act
        var result = Sponsor.Create(
            name: "Joe's Sponsorship Company",
            isCurrentSponsor: SponsorFactory.ValidIsCurrentSponsor,
            priority: SponsorFactory.ValidPriority,
            tier: SponsorFactory.ValidTier,
            category: SponsorFactory.ValidCategory,
            slug: "   ");

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SponsorErrors.SlugInvalid);
    }

    [Fact(DisplayName = "Create should normalize an explicitly provided slug")]
    public void Create_ShouldNormalizeExplicitSlug_WhenSlugIsProvided()
    {
        // Arrange & Act
        var result = Sponsor.Create(
            name: SponsorFactory.ValidName,
            isCurrentSponsor: SponsorFactory.ValidIsCurrentSponsor,
            priority: SponsorFactory.ValidPriority,
            tier: SponsorFactory.ValidTier,
            category: SponsorFactory.ValidCategory,
            slug: "  Custom Slug!! ");

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Slug.ShouldBe("custom-slug");
    }

    [Fact(DisplayName = "Create should assign a new ID when ID is not provided")]
    public void Create_ShouldAssignNewId_WhenIdIsNotProvided()
    {
        // Arrange & Act
        var result = Sponsor.Create(
            name: SponsorFactory.ValidName,
            isCurrentSponsor: SponsorFactory.ValidIsCurrentSponsor,
            priority: SponsorFactory.ValidPriority,
            tier: SponsorFactory.ValidTier,
            category: SponsorFactory.ValidCategory);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Id.ShouldNotBe(SponsorId.Empty);
    }

    [Fact(DisplayName = "Create should assign the provided ID when ID is given")]
    public void Create_ShouldAssignProvidedId_WhenIdIsGiven()
    {
        // Arrange
        var id = SponsorId.New();

        // Act
        var result = Sponsor.Create(
            name: SponsorFactory.ValidName,
            isCurrentSponsor: SponsorFactory.ValidIsCurrentSponsor,
            priority: SponsorFactory.ValidPriority,
            tier: SponsorFactory.ValidTier,
            category: SponsorFactory.ValidCategory,
            id: id);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Id.ShouldBe(id);
    }

    [Fact(DisplayName = "Create should default phone numbers to an empty collection when not provided")]
    public void Create_ShouldDefaultPhoneNumbersToEmptyCollection_WhenNotProvided()
    {
        // Arrange & Act
        var result = Sponsor.Create(
            name: SponsorFactory.ValidName,
            isCurrentSponsor: SponsorFactory.ValidIsCurrentSponsor,
            priority: SponsorFactory.ValidPriority,
            tier: SponsorFactory.ValidTier,
            category: SponsorFactory.ValidCategory,
            phoneNumbers: null);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.PhoneNumbers.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Create should assign all fields when inputs are valid")]
    public void Create_ShouldAssignAllFields_WhenInputsAreValid()
    {
        // Arrange
        var logo = StoredFileFactory.Create();
        var websiteUrl = new Uri("https://example.com");
        var facebookUrl = new Uri("https://facebook.com/example");
        var instagramUrl = new Uri("https://instagram.com/example");
        var businessAddress = AddressFactory.CreateUsAddress();
        var businessEmail = EmailAddressFactory.Create();
        var phoneNumbers = new[] { PhoneNumberFactory.Create() };
        var sponsorContact = ContactInfoFactory.Create();

        // Act
        var result = Sponsor.Create(
            name: SponsorFactory.ValidName,
            isCurrentSponsor: SponsorFactory.ValidIsCurrentSponsor,
            priority: SponsorFactory.ValidPriority,
            tier: SponsorFactory.ValidTier,
            category: SponsorFactory.ValidCategory,
            slug: SponsorFactory.ValidSlug,
            logo: logo,
            websiteUrl: websiteUrl,
            tagPhrase: "Great sponsor",
            description: "A description",
            liveReadText: "Live read text",
            promotionalNotes: "Promotional notes",
            facebookUrl: facebookUrl,
            instagramUrl: instagramUrl,
            businessAddress: businessAddress,
            businessEmail: businessEmail,
            phoneNumbers: phoneNumbers,
            sponsorContact: sponsorContact);

        // Assert
        result.IsError.ShouldBeFalse();
        var sponsor = result.Value;
        sponsor.Name.ShouldBe(SponsorFactory.ValidName);
        sponsor.Slug.ShouldBe(SponsorFactory.ValidSlug);
        sponsor.IsCurrentSponsor.ShouldBe(SponsorFactory.ValidIsCurrentSponsor);
        sponsor.Priority.ShouldBe(SponsorFactory.ValidPriority);
        sponsor.Tier.ShouldBe(SponsorFactory.ValidTier);
        sponsor.Category.ShouldBe(SponsorFactory.ValidCategory);
        sponsor.Logo.ShouldBe(logo);
        sponsor.WebsiteUrl.ShouldBe(websiteUrl);
        sponsor.TagPhrase.ShouldBe("Great sponsor");
        sponsor.Description.ShouldBe("A description");
        sponsor.LiveReadText.ShouldBe("Live read text");
        sponsor.PromotionalNotes.ShouldBe("Promotional notes");
        sponsor.FacebookUrl.ShouldBe(facebookUrl);
        sponsor.InstagramUrl.ShouldBe(instagramUrl);
        sponsor.BusinessAddress.ShouldBe(businessAddress);
        sponsor.BusinessEmail.ShouldBe(businessEmail);
        sponsor.PhoneNumbers.ShouldBe(phoneNumbers);
        sponsor.SponsorContact.ShouldBe(sponsorContact);
    }
}
