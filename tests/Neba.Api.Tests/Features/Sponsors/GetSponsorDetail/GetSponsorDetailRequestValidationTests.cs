using FluentAssertions;

using Neba.Api.Features.Sponsors.GetSponsorDetail;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Sponsors.GetSponsorDetail;

[UnitTest]
[Component("Sponsors")]
public sealed class GetSponsorDetailRequestValidationTests
{
    [Fact(DisplayName = "Validate should return slug required error when slug is null")]
    public void Validate_ShouldReturnSlugRequiredError_WhenSlugIsNull()
    {
#nullable disable
        // Arrange
        var validator = new GetSponsorDetailRequestValidation();
        var request = new GetSponsorDetailRequest { Slug = null };

        // Act
        var result = validator.Validate(request);
#nullable enable

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(GetSponsorDetailRequest.Slug)
            && error.ErrorCode == "SponsorDetailRequest.SlugRequired"
            && error.ErrorMessage == "Sponsor slug is required.");
    }

    [Theory(DisplayName = "Validate should return slug required error when slug is empty")]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldReturnSlugRequiredError_WhenSlugIsEmpty(string slug)
    {
        // Arrange
        var validator = new GetSponsorDetailRequestValidation();
        var request = new GetSponsorDetailRequest { Slug = slug };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(GetSponsorDetailRequest.Slug)
            && error.ErrorCode == "SponsorDetailRequest.SlugRequired"
            && error.ErrorMessage == "Sponsor slug is required.");
    }

    [Fact(DisplayName = "Validate should succeed when slug has a value")]
    public void Validate_ShouldSucceed_WhenSlugHasAValue()
    {
        // Arrange
        var validator = new GetSponsorDetailRequestValidation();
        var request = new GetSponsorDetailRequest { Slug = "my-sponsor" };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}