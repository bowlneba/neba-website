using Neba.Api.Features.Bowlers.SearchBowlers;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Bowlers.SearchBowlers;

[UnitTest]
[Component("Bowlers")]
public sealed class SearchBowlersRequestValidatorTests
{
    private readonly SearchBowlersRequestValidator _validator = new();

    [Theory(DisplayName = "Validate should succeed for a name of 3 to 100 characters")]
    [InlineData("abc", TestDisplayName = "Three characters")]
    [InlineData("Dave Smith", TestDisplayName = "A full name")]
    public void Validate_ShouldSucceed_WhenNameLengthIsValid(string name)
    {
        // Arrange
        var request = new SearchBowlersRequest { Name = name };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact(DisplayName = "Validate should succeed for a name of exactly 100 characters")]
    public void Validate_ShouldSucceed_WhenNameIsOneHundredCharacters()
    {
        // Arrange
        var request = new SearchBowlersRequest { Name = new string('a', 100) };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory(DisplayName = "Validate should fail with NameRequired when the name is empty")]
    [InlineData("", TestDisplayName = "Empty")]
    [InlineData("   ", TestDisplayName = "Whitespace")]
    public void Validate_ShouldFailWithNameRequired_WhenNameIsEmpty(string name)
    {
        // Arrange
        var request = new SearchBowlersRequest { Name = name };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.Errors.ShouldContain(error => error.ErrorCode == "SearchBowlersRequest.NameRequired");
    }

    [Theory(DisplayName = "Validate should fail with NameTooShort when the name is under 3 characters")]
    [InlineData("a", TestDisplayName = "One character")]
    [InlineData("ab", TestDisplayName = "Two characters")]
    public void Validate_ShouldFailWithNameTooShort_WhenNameIsShort(string name)
    {
        // Arrange
        var request = new SearchBowlersRequest { Name = name };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.Errors.ShouldContain(error => error.ErrorCode == "SearchBowlersRequest.NameTooShort");
    }

    [Fact(DisplayName = "Validate should fail with NameTooLong when the name is over 100 characters")]
    public void Validate_ShouldFailWithNameTooLong_WhenNameIsLong()
    {
        // Arrange
        var request = new SearchBowlersRequest { Name = new string('a', 101) };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.Errors.ShouldContain(error => error.ErrorCode == "SearchBowlersRequest.NameTooLong");
    }
}
