using Neba.Api.Contracts.Security.ResetPassword;
using Neba.Api.Security.Password.ResetPassword;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Security.Password.ResetPassword;

[UnitTest]
[Component("Security")]
public sealed class ResetPasswordRequestValidatorTests
{
    private readonly ResetPasswordRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should succeed when user ID is a valid ULID")]
    public void Validate_ShouldSucceed_WhenUserIdIsValid()
    {
        // Arrange
        var request = ResetPasswordRequestFactory.Create();

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Validate should fail with UserIdRequired when user ID is null")]
    public void Validate_ShouldFailWithUserIdRequired_WhenUserIdIsNull()
    {
        // Arrange
#nullable disable
        var request = new ResetPasswordRequest { UserId = null };
#nullable enable

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(ResetPasswordRequest.UserId)
            && e.ErrorCode == "ResetPasswordRequest.UserIdRequired"
            && e.ErrorMessage == "User ID is required.");
    }

    [Theory(DisplayName = "Validate should fail with UserIdRequired when user ID is empty or whitespace")]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFailWithUserIdRequired_WhenUserIdIsEmptyOrWhitespace(string userId)
    {
        // Arrange
        var request = new ResetPasswordRequest { UserId = userId };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(ResetPasswordRequest.UserId)
            && e.ErrorCode == "ResetPasswordRequest.UserIdRequired"
            && e.ErrorMessage == "User ID is required.");
    }

    [Theory(DisplayName = "Validate should fail with UserIdInvalid when user ID is not a valid ULID")]
    [InlineData("not-a-ulid")]
    [InlineData("12345")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Validate_ShouldFailWithUserIdInvalid_WhenUserIdIsNotAValidUlid(string userId)
    {
        // Arrange
        var request = new ResetPasswordRequest { UserId = userId };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(ResetPasswordRequest.UserId)
            && e.ErrorCode == "ResetPasswordRequest.UserIdInvalid"
            && e.ErrorMessage == "User ID must be a valid ULID.");
    }
}
