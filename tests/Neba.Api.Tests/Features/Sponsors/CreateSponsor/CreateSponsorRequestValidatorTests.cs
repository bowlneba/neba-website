using Neba.Api.Contracts.Sponsors.CreateSponsor;
using Neba.Api.Features.Sponsors.CreateSponsor;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Sponsors;

namespace Neba.Api.Tests.Features.Sponsors.CreateSponsor;

[UnitTest]
[Component("Sponsors")]
public sealed class CreateSponsorRequestValidatorTests
{
    private readonly CreateSponsorRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should succeed when all fields are valid")]
    public void Validate_ShouldSucceed_WhenAllFieldsAreValid()
    {
        // Arrange
        var request = new CreateSponsorRequest { Sponsor = SponsorInputFactory.Create() };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Validate should fail with NameRequired when Name is empty")]
    public void Validate_ShouldFailWithNameRequired_WhenNameIsEmpty()
    {
        // Arrange
        var request = new CreateSponsorRequest { Sponsor = SponsorInputFactory.Create(name: string.Empty) };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateSponsorRequest.NameRequired");
    }

    [Fact(DisplayName = "Validate should fail with NameTooLong when Name exceeds 63 characters")]
    public void Validate_ShouldFailWithNameTooLong_WhenNameExceeds63Characters()
    {
        // Arrange
        var request = new CreateSponsorRequest { Sponsor = SponsorInputFactory.Create(name: new string('a', 64)) };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateSponsorRequest.NameTooLong");
    }

    [Fact(DisplayName = "Validate should fail with SlugTooLong when a supplied Slug exceeds 63 characters")]
    public void Validate_ShouldFailWithSlugTooLong_WhenSlugExceeds63Characters()
    {
        // Arrange
        var request = new CreateSponsorRequest { Sponsor = SponsorInputFactory.Create(slug: new string('a', 64)) };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateSponsorRequest.SlugTooLong");
    }

    [Fact(DisplayName = "Validate should succeed when Slug is not supplied")]
    public void Validate_ShouldSucceed_WhenSlugIsNotSupplied()
    {
        // Arrange
        var request = new CreateSponsorRequest { Sponsor = SponsorInputFactory.Create(slug: null) };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact(DisplayName = "Validate should fail with TierRequired when Tier is empty")]
    public void Validate_ShouldFailWithTierRequired_WhenTierIsEmpty()
    {
        // Arrange
        var request = new CreateSponsorRequest { Sponsor = SponsorInputFactory.Create(tier: string.Empty) };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateSponsorRequest.TierRequired");
    }

    [Fact(DisplayName = "Validate should fail with TierInvalid when Tier is not a known tier")]
    public void Validate_ShouldFailWithTierInvalid_WhenTierIsUnknown()
    {
        // Arrange
        var request = new CreateSponsorRequest { Sponsor = SponsorInputFactory.Create(tier: "NotATier") };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateSponsorRequest.TierInvalid");
    }

    [Fact(DisplayName = "Validate should fail with CategoryRequired when Category is empty")]
    public void Validate_ShouldFailWithCategoryRequired_WhenCategoryIsEmpty()
    {
        // Arrange
        var request = new CreateSponsorRequest { Sponsor = SponsorInputFactory.Create(category: string.Empty) };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateSponsorRequest.CategoryRequired");
    }

    [Fact(DisplayName = "Validate should fail with CategoryInvalid when Category is not a known category")]
    public void Validate_ShouldFailWithCategoryInvalid_WhenCategoryIsUnknown()
    {
        // Arrange
        var request = new CreateSponsorRequest { Sponsor = SponsorInputFactory.Create(category: "NotACategory") };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateSponsorRequest.CategoryInvalid");
    }

    [Fact(DisplayName = "Validate should fail with WebsiteUrlInvalid when WebsiteUrl is a relative URI")]
    public void Validate_ShouldFailWithWebsiteUrlInvalid_WhenWebsiteUrlIsRelative()
    {
        // Arrange
        var request = new CreateSponsorRequest
        {
            Sponsor = SponsorInputFactory.Create(websiteUrl: new Uri("/relative", UriKind.Relative))
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateSponsorRequest.WebsiteUrlInvalid");
    }

    [Fact(DisplayName = "Validate should fail with ContactIncomplete when only some contact fields are supplied")]
    public void Validate_ShouldFailWithContactIncomplete_WhenOnlySomeContactFieldsAreSupplied()
    {
        // Arrange
        var request = new CreateSponsorRequest
        {
            Sponsor = SponsorInputFactory.Create(contact: new SponsorContactInput
            {
                Name = "Jane Doe",
                PhoneNumberType = "M",
                PhoneNumber = string.Empty,
                Email = string.Empty
            })
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateSponsorRequest.ContactIncomplete");
    }

    [Fact(DisplayName = "Validate should succeed when all contact fields are supplied")]
    public void Validate_ShouldSucceed_WhenAllContactFieldsAreSupplied()
    {
        // Arrange
        var request = new CreateSponsorRequest
        {
            Sponsor = SponsorInputFactory.Create(contact: new SponsorContactInput
            {
                Name = "Jane Doe",
                PhoneNumberType = "M",
                PhoneNumber = "5559876543",
                Email = "jane@example.com"
            })
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }
}
