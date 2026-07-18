using Neba.Api.Contracts.Sponsors.CreateSponsor;
using Neba.Api.Contracts.Sponsors.EditSponsor;
using Neba.Api.Features.Sponsors.EditSponsor;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Sponsors;

namespace Neba.Api.Tests.Features.Sponsors.EditSponsor;

[UnitTest]
[Component("Sponsors")]
public sealed class EditSponsorRequestValidatorTests
{
    private readonly EditSponsorRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should succeed when all fields are valid")]
    public void Validate_ShouldSucceed_WhenAllFieldsAreValid()
    {
        // Arrange
        var request = EditSponsorRequestFactory.Create();

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Validate should fail with IdRequired when Id is empty")]
    public void Validate_ShouldFailWithIdRequired_WhenIdIsEmpty()
    {
        // Arrange
        var request = EditSponsorRequestFactory.Create(id: string.Empty);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "EditSponsorRequest.IdRequired");
    }

    [Fact(DisplayName = "Validate should fail with IdInvalidLength when Id is not 26 characters")]
    public void Validate_ShouldFailWithIdInvalidLength_WhenIdIsNot26Characters()
    {
        // Arrange
        var request = EditSponsorRequestFactory.Create(id: "too-short");

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "EditSponsorRequest.IdInvalidLength");
    }

    [Fact(DisplayName = "Validate should fail with NameRequired when Name is empty")]
    public void Validate_ShouldFailWithNameRequired_WhenNameIsEmpty()
    {
        // Arrange
        var request = EditSponsorRequestFactory.Create(sponsor: EditSponsorInputFactory.Create(name: string.Empty));

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "EditSponsorRequest.NameRequired");
    }

    [Fact(DisplayName = "Validate should fail with NameTooLong when Name exceeds 63 characters")]
    public void Validate_ShouldFailWithNameTooLong_WhenNameExceeds63Characters()
    {
        // Arrange
        var request = EditSponsorRequestFactory.Create(sponsor: EditSponsorInputFactory.Create(name: new string('a', 64)));

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "EditSponsorRequest.NameTooLong");
    }

    [Fact(DisplayName = "Validate should fail with TierRequired when Tier is empty")]
    public void Validate_ShouldFailWithTierRequired_WhenTierIsEmpty()
    {
        // Arrange
        var request = EditSponsorRequestFactory.Create(sponsor: EditSponsorInputFactory.Create(tier: string.Empty));

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "EditSponsorRequest.TierRequired");
    }

    [Fact(DisplayName = "Validate should fail with TierInvalid when Tier is not a known tier")]
    public void Validate_ShouldFailWithTierInvalid_WhenTierIsUnknown()
    {
        // Arrange
        var request = EditSponsorRequestFactory.Create(sponsor: EditSponsorInputFactory.Create(tier: "NotATier"));

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "EditSponsorRequest.TierInvalid");
    }

    [Fact(DisplayName = "Validate should fail with CategoryRequired when Category is empty")]
    public void Validate_ShouldFailWithCategoryRequired_WhenCategoryIsEmpty()
    {
        // Arrange
        var request = EditSponsorRequestFactory.Create(sponsor: EditSponsorInputFactory.Create(category: string.Empty));

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "EditSponsorRequest.CategoryRequired");
    }

    [Fact(DisplayName = "Validate should fail with CategoryInvalid when Category is not a known category")]
    public void Validate_ShouldFailWithCategoryInvalid_WhenCategoryIsUnknown()
    {
        // Arrange
        var request = EditSponsorRequestFactory.Create(sponsor: EditSponsorInputFactory.Create(category: "NotACategory"));

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "EditSponsorRequest.CategoryInvalid");
    }

    [Fact(DisplayName = "Validate should fail with WebsiteUrlInvalid when WebsiteUrl is a relative URI")]
    public void Validate_ShouldFailWithWebsiteUrlInvalid_WhenWebsiteUrlIsRelative()
    {
        // Arrange
        var request = EditSponsorRequestFactory.Create(
            sponsor: EditSponsorInputFactory.Create(websiteUrl: new Uri("/relative", UriKind.Relative)));

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "EditSponsorRequest.WebsiteUrlInvalid");
    }

    [Fact(DisplayName = "Validate should fail with FacebookUrlInvalid when FacebookUrl is a relative URI")]
    public void Validate_ShouldFailWithFacebookUrlInvalid_WhenFacebookUrlIsRelative()
    {
        // Arrange
        var request = EditSponsorRequestFactory.Create(
            sponsor: EditSponsorInputFactory.Create(facebookUrl: new Uri("/relative", UriKind.Relative)));

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "EditSponsorRequest.FacebookUrlInvalid");
    }

    [Fact(DisplayName = "Validate should fail with InstagramUrlInvalid when InstagramUrl is a relative URI")]
    public void Validate_ShouldFailWithInstagramUrlInvalid_WhenInstagramUrlIsRelative()
    {
        // Arrange
        var request = EditSponsorRequestFactory.Create(
            sponsor: EditSponsorInputFactory.Create(instagramUrl: new Uri("/relative", UriKind.Relative)));

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "EditSponsorRequest.InstagramUrlInvalid");
    }

    [Fact(DisplayName = "Validate should fail with ContactIncomplete when only some contact fields are supplied")]
    public void Validate_ShouldFailWithContactIncomplete_WhenOnlySomeContactFieldsAreSupplied()
    {
        // Arrange
        var request = EditSponsorRequestFactory.Create(sponsor: EditSponsorInputFactory.Create(contact: new SponsorContactInput
        {
            Name = "Jane Doe",
            PhoneNumberType = "M",
            PhoneNumber = string.Empty,
            Email = string.Empty
        }));

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "EditSponsorRequest.ContactIncomplete");
    }

    [Fact(DisplayName = "Validate should succeed when all contact fields are supplied")]
    public void Validate_ShouldSucceed_WhenAllContactFieldsAreSupplied()
    {
        // Arrange
        var request = EditSponsorRequestFactory.Create(sponsor: EditSponsorInputFactory.Create(contact: new SponsorContactInput
        {
            Name = "Jane Doe",
            PhoneNumberType = "M",
            PhoneNumber = "5559876543",
            Email = "jane@example.com"
        }));

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }
}