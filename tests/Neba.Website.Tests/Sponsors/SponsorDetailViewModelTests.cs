using Neba.TestFactory.Attributes;
using Neba.TestFactory.Sponsors;

namespace Neba.Website.Tests.Sponsors;

[UnitTest]
[Component("Website.Sponsors.SponsorDetailViewModel")]
public sealed class SponsorDetailViewModelTests
{
    // ── HasAddress ─────────────────────────────────────────────────────────────

    [Fact(DisplayName = "HasAddress should be true when BusinessStreet is provided")]
    public void HasAddress_ShouldBeTrue_WhenBusinessStreetIsProvided()
    {
        // Arrange
        var vm = SponsorDetailViewModelFactory.Create(businessStreet: "123 Main St", businessCity: null);

        // Assert
        vm.HasAddress.ShouldBeTrue();
    }

    [Fact(DisplayName = "HasAddress should be true when BusinessCity is provided")]
    public void HasAddress_ShouldBeTrue_WhenBusinessCityIsProvided()
    {
        // Arrange
        var vm = SponsorDetailViewModelFactory.Create(businessStreet: null, businessCity: "Anytown");

        // Assert
        vm.HasAddress.ShouldBeTrue();
    }

    [Fact(DisplayName = "HasAddress should be false when both BusinessStreet and BusinessCity are null")]
    public void HasAddress_ShouldBeFalse_WhenBothAddressFieldsAreNull()
    {
        // Arrange
        var vm = SponsorDetailViewModelFactory.Create() with { BusinessStreet = null, BusinessCity = null };

        // Assert
        vm.HasAddress.ShouldBeFalse();
    }

    // ── HasContactChannels ─────────────────────────────────────────────────────

    [Fact(DisplayName = "HasContactChannels should be true when ContactEmail is provided")]
    public void HasContactChannels_ShouldBeTrue_WhenContactEmailIsProvided()
    {
        // Arrange
        var vm = SponsorDetailViewModelFactory.Create(contactEmail: "test@example.com", phoneNumbers: []);

        // Assert
        vm.HasContactChannels.ShouldBeTrue();
    }

    [Fact(DisplayName = "HasContactChannels should be true when phone numbers are present")]
    public void HasContactChannels_ShouldBeTrue_WhenPhoneNumbersArePresent()
    {
        // Arrange
        var vm = SponsorDetailViewModelFactory.Create(contactEmail: null);

        // Assert
        vm.HasContactChannels.ShouldBeTrue();
    }

    [Fact(DisplayName = "HasContactChannels should be false when no email and no phone numbers")]
    public void HasContactChannels_ShouldBeFalse_WhenNoContactChannels()
    {
        // Arrange
        var vm = SponsorDetailViewModelFactory.Create(phoneNumbers: []) with { ContactEmail = null };

        // Assert
        vm.HasContactChannels.ShouldBeFalse();
    }

    // ── HasSocialMedia ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "HasSocialMedia should be true when FacebookUrl is provided")]
    public void HasSocialMedia_ShouldBeTrue_WhenFacebookUrlIsProvided()
    {
        // Arrange
        var vm = SponsorDetailViewModelFactory.Create(
            facebookUrl: new Uri("https://facebook.com/test"),
            instagramUrl: null);

        // Assert
        vm.HasSocialMedia.ShouldBeTrue();
    }

    [Fact(DisplayName = "HasSocialMedia should be true when InstagramUrl is provided")]
    public void HasSocialMedia_ShouldBeTrue_WhenInstagramUrlIsProvided()
    {
        // Arrange
        var vm = SponsorDetailViewModelFactory.Create(
            facebookUrl: null,
            instagramUrl: new Uri("https://instagram.com/test"));

        // Assert
        vm.HasSocialMedia.ShouldBeTrue();
    }

    [Fact(DisplayName = "HasSocialMedia should be false when no social media urls are provided")]
    public void HasSocialMedia_ShouldBeFalse_WhenNoSocialUrls()
    {
        // Arrange
        var vm = SponsorDetailViewModelFactory.Create(facebookUrl: null, instagramUrl: null);

        // Assert
        vm.HasSocialMedia.ShouldBeFalse();
    }

}