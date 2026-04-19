using Neba.TestFactory.Attributes;
using Neba.TestFactory.Sponsors;
using Neba.Website.Server.Sponsors;

namespace Neba.Website.Tests.Sponsors;

[UnitTest]
[Component("Website.Sponsors.SponsorDetailViewModel")]
public sealed class SponsorDetailViewModelTests
{
    // ── HasAddress ─────────────────────────────────────────────────────────────

    [Fact(DisplayName = "HasAddress should be true when BusinessStreet is provided")]
    public void HasAddress_ShouldBeTrue_WhenBusinessStreetIsProvided()
    {
        var vm = SponsorDetailViewModelFactory.Create(businessStreet: "123 Main St", businessCity: null);
        vm.HasAddress.ShouldBeTrue();
    }

    [Fact(DisplayName = "HasAddress should be true when BusinessCity is provided")]
    public void HasAddress_ShouldBeTrue_WhenBusinessCityIsProvided()
    {
        var vm = SponsorDetailViewModelFactory.Create(businessStreet: null, businessCity: "Anytown");
        vm.HasAddress.ShouldBeTrue();
    }

    [Fact(DisplayName = "HasAddress should be false when both BusinessStreet and BusinessCity are null")]
    public void HasAddress_ShouldBeFalse_WhenBothAddressFieldsAreNull()
    {
        var vm = SponsorDetailViewModelFactory.Create() with { BusinessStreet = null, BusinessCity = null };
        vm.HasAddress.ShouldBeFalse();
    }

    // ── HasContactChannels ─────────────────────────────────────────────────────

    [Fact(DisplayName = "HasContactChannels should be true when ContactEmail is provided")]
    public void HasContactChannels_ShouldBeTrue_WhenContactEmailIsProvided()
    {
        var vm = SponsorDetailViewModelFactory.Create(contactEmail: "test@example.com", phoneNumbers: []);
        vm.HasContactChannels.ShouldBeTrue();
    }

    [Fact(DisplayName = "HasContactChannels should be true when phone numbers are present")]
    public void HasContactChannels_ShouldBeTrue_WhenPhoneNumbersArePresent()
    {
        var vm = SponsorDetailViewModelFactory.Create(contactEmail: null);
        vm.HasContactChannels.ShouldBeTrue();
    }

    [Fact(DisplayName = "HasContactChannels should be false when no email and no phone numbers")]
    public void HasContactChannels_ShouldBeFalse_WhenNoContactChannels()
    {
        var vm = SponsorDetailViewModelFactory.Create(phoneNumbers: []) with { ContactEmail = null };
        vm.HasContactChannels.ShouldBeFalse();
    }

    // ── HasSocialMedia ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "HasSocialMedia should be true when FacebookUrl is provided")]
    public void HasSocialMedia_ShouldBeTrue_WhenFacebookUrlIsProvided()
    {
        var vm = SponsorDetailViewModelFactory.Create(
            facebookUrl: new Uri("https://facebook.com/test"),
            instagramUrl: null);
        vm.HasSocialMedia.ShouldBeTrue();
    }

    [Fact(DisplayName = "HasSocialMedia should be true when InstagramUrl is provided")]
    public void HasSocialMedia_ShouldBeTrue_WhenInstagramUrlIsProvided()
    {
        var vm = SponsorDetailViewModelFactory.Create(
            facebookUrl: null,
            instagramUrl: new Uri("https://instagram.com/test"));
        vm.HasSocialMedia.ShouldBeTrue();
    }

    [Fact(DisplayName = "HasSocialMedia should be false when no social media urls are provided")]
    public void HasSocialMedia_ShouldBeFalse_WhenNoSocialUrls()
    {
        var vm = SponsorDetailViewModelFactory.Create(facebookUrl: null, instagramUrl: null);
        vm.HasSocialMedia.ShouldBeFalse();
    }

    // ── HasPromotionalInfo ─────────────────────────────────────────────────────

    [Fact(DisplayName = "HasPromotionalInfo should be true when PromotionalNotes is provided")]
    public void HasPromotionalInfo_ShouldBeTrue_WhenPromotionalNotesIsProvided()
    {
        var vm = SponsorDetailViewModelFactory.Create(promotionalNotes: "Some notes", liveReadScript: null);
        vm.HasPromotionalInfo.ShouldBeTrue();
    }

    [Fact(DisplayName = "HasPromotionalInfo should be true when LiveReadScript is provided")]
    public void HasPromotionalInfo_ShouldBeTrue_WhenLiveReadScriptIsProvided()
    {
        var vm = SponsorDetailViewModelFactory.Create(promotionalNotes: null, liveReadScript: "Read this.");
        vm.HasPromotionalInfo.ShouldBeTrue();
    }

    [Fact(DisplayName = "HasPromotionalInfo should be false when neither field is provided")]
    public void HasPromotionalInfo_ShouldBeFalse_WhenNoPromotionalInfo()
    {
        var vm = SponsorDetailViewModelFactory.Create(promotionalNotes: null, liveReadScript: null);
        vm.HasPromotionalInfo.ShouldBeFalse();
    }

    // ── HasInternalContact ─────────────────────────────────────────────────────

    [Fact(DisplayName = "HasInternalContact should be true when SponsorContactName is provided")]
    public void HasInternalContact_ShouldBeTrue_WhenSponsorContactNameIsProvided()
    {
        var vm = SponsorDetailViewModelFactory.Create(sponsorContactName: "Jane Doe");
        vm.HasInternalContact.ShouldBeTrue();
    }

    [Fact(DisplayName = "HasInternalContact should be false when SponsorContactName is null")]
    public void HasInternalContact_ShouldBeFalse_WhenSponsorContactNameIsNull()
    {
        var vm = SponsorDetailViewModelFactory.Create(sponsorContactName: null);
        vm.HasInternalContact.ShouldBeFalse();
    }
}
