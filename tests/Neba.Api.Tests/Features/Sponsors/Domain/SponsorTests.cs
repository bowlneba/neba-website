using Neba.Api.Contacts.Domain;
using Neba.Api.Features.Sponsors;
using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Contact;
using Neba.TestFactory.Sponsors;
using Neba.TestFactory.Storage;
using Neba.TestFactory.Tournaments;

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

    [Fact(DisplayName = "Create should assign a new ID")]
    public void Create_ShouldAssignNewId()
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

    [Fact(DisplayName = "Create should return an error when tier is TitleSponsor and Title sponsorship is unavailable")]
    public void Create_ShouldReturnError_WhenTierIsTitleSponsorAndTitleSponsorshipIsUnavailable()
    {
        // Arrange & Act
        var result = Sponsor.Create(
            name: SponsorFactory.ValidName,
            isCurrentSponsor: SponsorFactory.ValidIsCurrentSponsor,
            priority: SponsorFactory.ValidPriority,
            tier: SponsorTier.TitleSponsor,
            category: SponsorFactory.ValidCategory,
            isTitleSponsorshipAvailable: false);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SponsorErrors.TitleSponsorshipUnavailable);
    }

    [Fact(DisplayName = "Create should return an error when tier is TitleSponsor and isTitleSponsorshipAvailable is omitted")]
    public void Create_ShouldReturnError_WhenTierIsTitleSponsorAndIsTitleSponsorshipAvailableIsOmitted()
    {
        // Arrange & Act
        var result = Sponsor.Create(
            name: SponsorFactory.ValidName,
            isCurrentSponsor: SponsorFactory.ValidIsCurrentSponsor,
            priority: SponsorFactory.ValidPriority,
            tier: SponsorTier.TitleSponsor,
            category: SponsorFactory.ValidCategory);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SponsorErrors.TitleSponsorshipUnavailable);
    }

    [Fact(DisplayName = "Create should succeed when tier is TitleSponsor and Title sponsorship is available")]
    public void Create_ShouldSucceed_WhenTierIsTitleSponsorAndTitleSponsorshipIsAvailable()
    {
        // Arrange & Act
        var result = Sponsor.Create(
            name: SponsorFactory.ValidName,
            isCurrentSponsor: SponsorFactory.ValidIsCurrentSponsor,
            priority: SponsorFactory.ValidPriority,
            tier: SponsorTier.TitleSponsor,
            category: SponsorFactory.ValidCategory,
            isTitleSponsorshipAvailable: true);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Tier.ShouldBe(SponsorTier.TitleSponsor);
    }

    [Theory(DisplayName = "Create should succeed regardless of isTitleSponsorshipAvailable when tier is not TitleSponsor")]
    [InlineData(true)]
    [InlineData(false)]
    public void Create_ShouldSucceed_WhenTierIsNotTitleSponsorRegardlessOfTitleSponsorshipAvailability(bool isTitleSponsorshipAvailable)
    {
        // Arrange & Act
        var result = Sponsor.Create(
            name: SponsorFactory.ValidName,
            isCurrentSponsor: SponsorFactory.ValidIsCurrentSponsor,
            priority: SponsorFactory.ValidPriority,
            tier: SponsorFactory.ValidTier,
            category: SponsorFactory.ValidCategory,
            isTitleSponsorshipAvailable: isTitleSponsorshipAvailable);

        // Assert
        result.IsError.ShouldBeFalse();
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

    // ── Update ─────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Update should return an error when name is null")]
    public void Update_ShouldReturnError_WhenNameIsNull()
    {
        // Arrange
        var sponsor = SponsorFactory.Create();

#nullable disable
        // Act
        var result = sponsor.Update(
            name: null,
            isCurrentSponsor: SponsorFactory.ValidIsCurrentSponsor,
            priority: SponsorFactory.ValidPriority,
            tier: SponsorFactory.ValidTier,
            category: SponsorFactory.ValidCategory,
            isTitleSponsorshipAvailable: false,
            logo: null,
            websiteUrl: null,
            tagPhrase: null,
            description: null,
            liveReadText: null,
            promotionalNotes: null,
            facebookUrl: null,
            instagramUrl: null,
            businessAddress: null,
            businessEmail: null,
            phoneNumbers: [],
            sponsorContact: null);
#nullable enable

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SponsorErrors.NameRequired);
    }

    [Theory(DisplayName = "Update should return an error when name is empty or whitespace")]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_ShouldReturnError_WhenNameIsEmptyOrWhitespace(string name)
    {
        // Arrange
        var sponsor = SponsorFactory.Create();

        // Act
        var result = sponsor.Update(
            name: name,
            isCurrentSponsor: SponsorFactory.ValidIsCurrentSponsor,
            priority: SponsorFactory.ValidPriority,
            tier: SponsorFactory.ValidTier,
            category: SponsorFactory.ValidCategory,
            isTitleSponsorshipAvailable: false,
            logo: null,
            websiteUrl: null,
            tagPhrase: null,
            description: null,
            liveReadText: null,
            promotionalNotes: null,
            facebookUrl: null,
            instagramUrl: null,
            businessAddress: null,
            businessEmail: null,
            phoneNumbers: [],
            sponsorContact: null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SponsorErrors.NameRequired);
    }

    [Fact(DisplayName = "Update should leave existing state unchanged when name is invalid")]
    public void Update_ShouldLeaveStateUnchanged_WhenNameIsInvalid()
    {
        // Arrange
        var sponsor = SponsorFactory.Create(
            name: SponsorFactory.ValidName,
            priority: SponsorFactory.ValidPriority);

        // Act
        var result = sponsor.Update(
            name: string.Empty,
            isCurrentSponsor: !SponsorFactory.ValidIsCurrentSponsor,
            priority: SponsorFactory.ValidPriority + 1,
            tier: SponsorFactory.ValidTier,
            category: SponsorFactory.ValidCategory,
            isTitleSponsorshipAvailable: false,
            logo: null,
            websiteUrl: null,
            tagPhrase: null,
            description: null,
            liveReadText: null,
            promotionalNotes: null,
            facebookUrl: null,
            instagramUrl: null,
            businessAddress: null,
            businessEmail: null,
            phoneNumbers: [],
            sponsorContact: null);

        // Assert
        result.IsError.ShouldBeTrue();
        sponsor.Name.ShouldBe(SponsorFactory.ValidName);
        sponsor.Priority.ShouldBe(SponsorFactory.ValidPriority);
    }

    [Fact(DisplayName = "Update should return an error when tier is TitleSponsor and Title sponsorship is unavailable")]
    public void Update_ShouldReturnError_WhenTierIsTitleSponsorAndTitleSponsorshipIsUnavailable()
    {
        // Arrange
        var sponsor = SponsorFactory.Create();

        // Act
        var result = sponsor.Update(
            name: SponsorFactory.ValidName,
            isCurrentSponsor: SponsorFactory.ValidIsCurrentSponsor,
            priority: SponsorFactory.ValidPriority,
            tier: SponsorTier.TitleSponsor,
            category: SponsorFactory.ValidCategory,
            isTitleSponsorshipAvailable: false,
            logo: null,
            websiteUrl: null,
            tagPhrase: null,
            description: null,
            liveReadText: null,
            promotionalNotes: null,
            facebookUrl: null,
            instagramUrl: null,
            businessAddress: null,
            businessEmail: null,
            phoneNumbers: [],
            sponsorContact: null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SponsorErrors.TitleSponsorshipUnavailable);
    }

    [Fact(DisplayName = "Update should not change tier when TitleSponsor update is rejected")]
    public void Update_ShouldNotChangeTier_WhenTitleSponsorUpdateIsRejected()
    {
        // Arrange
        var sponsor = SponsorFactory.Create(tier: SponsorTier.Standard);

        // Act
        var result = sponsor.Update(
            name: SponsorFactory.ValidName,
            isCurrentSponsor: SponsorFactory.ValidIsCurrentSponsor,
            priority: SponsorFactory.ValidPriority,
            tier: SponsorTier.TitleSponsor,
            category: SponsorFactory.ValidCategory,
            isTitleSponsorshipAvailable: false,
            logo: null,
            websiteUrl: null,
            tagPhrase: null,
            description: null,
            liveReadText: null,
            promotionalNotes: null,
            facebookUrl: null,
            instagramUrl: null,
            businessAddress: null,
            businessEmail: null,
            phoneNumbers: [],
            sponsorContact: null);

        // Assert
        result.IsError.ShouldBeTrue();
        sponsor.Tier.ShouldBe(SponsorTier.Standard);
    }

    [Fact(DisplayName = "Update should succeed when tier is TitleSponsor and Title sponsorship is available")]
    public void Update_ShouldSucceed_WhenTierIsTitleSponsorAndTitleSponsorshipIsAvailable()
    {
        // Arrange
        var sponsor = SponsorFactory.Create(tier: SponsorTier.Standard);

        // Act
        var result = sponsor.Update(
            name: SponsorFactory.ValidName,
            isCurrentSponsor: SponsorFactory.ValidIsCurrentSponsor,
            priority: SponsorFactory.ValidPriority,
            tier: SponsorTier.TitleSponsor,
            category: SponsorFactory.ValidCategory,
            isTitleSponsorshipAvailable: true,
            logo: null,
            websiteUrl: null,
            tagPhrase: null,
            description: null,
            liveReadText: null,
            promotionalNotes: null,
            facebookUrl: null,
            instagramUrl: null,
            businessAddress: null,
            businessEmail: null,
            phoneNumbers: [],
            sponsorContact: null);

        // Assert
        result.IsError.ShouldBeFalse();
        sponsor.Tier.ShouldBe(SponsorTier.TitleSponsor);
    }

    [Theory(DisplayName = "Update should succeed regardless of isTitleSponsorshipAvailable when tier is not TitleSponsor")]
    [InlineData(true)]
    [InlineData(false)]
    public void Update_ShouldSucceed_WhenTierIsNotTitleSponsorRegardlessOfTitleSponsorshipAvailability(bool isTitleSponsorshipAvailable)
    {
        // Arrange
        var sponsor = SponsorFactory.Create();

        // Act
        var result = sponsor.Update(
            name: SponsorFactory.ValidName,
            isCurrentSponsor: SponsorFactory.ValidIsCurrentSponsor,
            priority: SponsorFactory.ValidPriority,
            tier: SponsorFactory.ValidTier,
            category: SponsorFactory.ValidCategory,
            isTitleSponsorshipAvailable: isTitleSponsorshipAvailable,
            logo: null,
            websiteUrl: null,
            tagPhrase: null,
            description: null,
            liveReadText: null,
            promotionalNotes: null,
            facebookUrl: null,
            instagramUrl: null,
            businessAddress: null,
            businessEmail: null,
            phoneNumbers: [],
            sponsorContact: null);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "Update should not change the Id or Slug")]
    public void Update_ShouldNotChangeIdOrSlug()
    {
        // Arrange
        var sponsor = SponsorFactory.Create();
        var originalId = sponsor.Id;
        var originalSlug = sponsor.Slug;

        // Act
        var result = sponsor.Update(
            name: "A Different Name",
            isCurrentSponsor: SponsorFactory.ValidIsCurrentSponsor,
            priority: SponsorFactory.ValidPriority,
            tier: SponsorFactory.ValidTier,
            category: SponsorFactory.ValidCategory,
            isTitleSponsorshipAvailable: false,
            logo: null,
            websiteUrl: null,
            tagPhrase: null,
            description: null,
            liveReadText: null,
            promotionalNotes: null,
            facebookUrl: null,
            instagramUrl: null,
            businessAddress: null,
            businessEmail: null,
            phoneNumbers: [],
            sponsorContact: null);

        // Assert
        result.IsError.ShouldBeFalse();
        sponsor.Id.ShouldBe(originalId);
        sponsor.Slug.ShouldBe(originalSlug);
    }

    [Fact(DisplayName = "Update should assign all fields when inputs are valid")]
    public void Update_ShouldAssignAllFields_WhenInputsAreValid()
    {
        // Arrange
        var sponsor = SponsorFactory.Create();
        var logo = StoredFileFactory.Create();
        var websiteUrl = new Uri("https://example.com/updated");
        var facebookUrl = new Uri("https://facebook.com/updated");
        var instagramUrl = new Uri("https://instagram.com/updated");
        var businessAddress = AddressFactory.CreateUsAddress();
        var businessEmail = EmailAddressFactory.Create();
        var phoneNumbers = new[] { PhoneNumberFactory.Create() };
        var sponsorContact = ContactInfoFactory.Create();

        // Act
        var result = sponsor.Update(
            name: "Updated Sponsor Name",
            isCurrentSponsor: !SponsorFactory.ValidIsCurrentSponsor,
            priority: SponsorFactory.ValidPriority + 1,
            tier: SponsorFactory.ValidTier,
            category: SponsorFactory.ValidCategory,
            isTitleSponsorshipAvailable: false,
            logo: logo,
            websiteUrl: websiteUrl,
            tagPhrase: "Updated tag phrase",
            description: "Updated description",
            liveReadText: "Updated live read text",
            promotionalNotes: "Updated promotional notes",
            facebookUrl: facebookUrl,
            instagramUrl: instagramUrl,
            businessAddress: businessAddress,
            businessEmail: businessEmail,
            phoneNumbers: phoneNumbers,
            sponsorContact: sponsorContact);

        // Assert
        result.IsError.ShouldBeFalse();
        sponsor.Name.ShouldBe("Updated Sponsor Name");
        sponsor.IsCurrentSponsor.ShouldBe(!SponsorFactory.ValidIsCurrentSponsor);
        sponsor.Priority.ShouldBe(SponsorFactory.ValidPriority + 1);
        sponsor.Tier.ShouldBe(SponsorFactory.ValidTier);
        sponsor.Category.ShouldBe(SponsorFactory.ValidCategory);
        sponsor.Logo.ShouldBe(logo);
        sponsor.WebsiteUrl.ShouldBe(websiteUrl);
        sponsor.TagPhrase.ShouldBe("Updated tag phrase");
        sponsor.Description.ShouldBe("Updated description");
        sponsor.LiveReadText.ShouldBe("Updated live read text");
        sponsor.PromotionalNotes.ShouldBe("Updated promotional notes");
        sponsor.FacebookUrl.ShouldBe(facebookUrl);
        sponsor.InstagramUrl.ShouldBe(instagramUrl);
        sponsor.BusinessAddress.ShouldBe(businessAddress);
        sponsor.BusinessEmail.ShouldBe(businessEmail);
        sponsor.PhoneNumbers.ShouldBe(phoneNumbers);
        sponsor.SponsorContact.ShouldBe(sponsorContact);
    }

    [Fact(DisplayName = "Update should clear optional fields when null is provided")]
    public void Update_ShouldClearOptionalFields_WhenNullIsProvided()
    {
        // Arrange
        var sponsor = SponsorFactory.Create(
            logo: StoredFileFactory.Create(),
            websiteUrl: new Uri("https://example.com"),
            tagPhrase: "Original tag phrase",
            description: "Original description",
            liveReadText: "Original live read text",
            promotionalNotes: "Original promotional notes",
            facebookUrl: new Uri("https://facebook.com/original"),
            instagramUrl: new Uri("https://instagram.com/original"),
            businessAddress: AddressFactory.CreateUsAddress(),
            businessEmail: EmailAddressFactory.Create(),
            sponsorContact: ContactInfoFactory.Create());

        // Act
        var result = sponsor.Update(
            name: SponsorFactory.ValidName,
            isCurrentSponsor: SponsorFactory.ValidIsCurrentSponsor,
            priority: SponsorFactory.ValidPriority,
            tier: SponsorFactory.ValidTier,
            category: SponsorFactory.ValidCategory,
            isTitleSponsorshipAvailable: false,
            logo: null,
            websiteUrl: null,
            tagPhrase: null,
            description: null,
            liveReadText: null,
            promotionalNotes: null,
            facebookUrl: null,
            instagramUrl: null,
            businessAddress: null,
            businessEmail: null,
            phoneNumbers: [],
            sponsorContact: null);

        // Assert
        result.IsError.ShouldBeFalse();
        sponsor.Logo.ShouldBeNull();
        sponsor.WebsiteUrl.ShouldBeNull();
        sponsor.TagPhrase.ShouldBeNull();
        sponsor.Description.ShouldBeNull();
        sponsor.LiveReadText.ShouldBeNull();
        sponsor.PromotionalNotes.ShouldBeNull();
        sponsor.FacebookUrl.ShouldBeNull();
        sponsor.InstagramUrl.ShouldBeNull();
        sponsor.BusinessAddress.ShouldBeNull();
        sponsor.BusinessEmail.ShouldBeNull();
        sponsor.PhoneNumbers.ShouldBeEmpty();
        sponsor.SponsorContact.ShouldBeNull();
    }

    // ── EF owned-collection fixup regression ────────────────────────────────
    // Guards Sponsor.PhoneNumbers/TournamentsSponsored against reverting from `new List<T>()` back
    // to the `[]` collection-expression form (e.g. via an IDE0305/IDE0028/SonarQube "simplify collection
    // initialization" suggestion). Target-typed to the IReadOnlyCollection<T> property, `[]` resolves
    // to a fixed-size T[] at runtime, and EF's owned-collection fixup throws NotSupportedException
    // when it tries to Add into that array while materializing the aggregate. See CLAUDE.md
    // "EF Core Navigation Fixup" for the full writeup.

    [Fact(DisplayName = "PhoneNumbers default instance (from Create) supports Add, as EF's owned-collection fixup requires")]
    public void PhoneNumbers_DefaultInstance_ShouldSupportAdd_ForEfFixup()
    {
        // Arrange
        var result = Sponsor.Create(
            name: SponsorFactory.ValidName,
            isCurrentSponsor: SponsorFactory.ValidIsCurrentSponsor,
            priority: SponsorFactory.ValidPriority,
            tier: SponsorFactory.ValidTier,
            category: SponsorFactory.ValidCategory);
        result.IsError.ShouldBeFalse();
        var mutablePhoneNumbers = (ICollection<PhoneNumber>)result.Value.PhoneNumbers;

        // Act & Assert — a fixed-size array throws NotSupportedException here instead
        Should.NotThrow(() => mutablePhoneNumbers.Add(PhoneNumberFactory.Create()));
    }

    [Fact(DisplayName = "PhoneNumbers property initializer default supports Add, as EF's owned-collection fixup requires")]
    public void PhoneNumbers_PropertyInitializerDefault_ShouldSupportAdd_ForEfFixup()
    {
        // Arrange
        var sponsor = new Sponsor
        {
            Id = SponsorId.New(),
            Slug = SponsorFactory.ValidSlug
        };
        var mutablePhoneNumbers = (ICollection<PhoneNumber>)sponsor.PhoneNumbers;

        // Act & Assert — a fixed-size array throws NotSupportedException here instead
        Should.NotThrow(() => mutablePhoneNumbers.Add(PhoneNumberFactory.Create()));
    }

    [Fact(DisplayName = "TournamentsSponsored property initializer default supports Add, as EF's owned-collection fixup requires")]
    public void TournamentsSponsored_PropertyInitializerDefault_ShouldSupportAdd_ForEfFixup()
    {
        // Arrange
        var sponsor = new Sponsor
        {
            Id = SponsorId.New(),
            Slug = SponsorFactory.ValidSlug
        };
        var mutableTournamentsSponsored = (ICollection<TournamentSponsor>)sponsor.TournamentsSponsored;
        var tournamentSponsor = TournamentSponsorFactory.Create();

        // Act & Assert — a fixed-size array throws NotSupportedException here instead
        Should.NotThrow(() => mutableTournamentsSponsored.Add(tournamentSponsor));
    }
}