using Neba.Api.Contracts.Security.RefreshToken;
using Neba.Api.Security.RefreshToken;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Security.RefreshToken;

[UnitTest]
[Component("Security")]
public sealed class RefreshTokenRequestValidatorTests
{
    private readonly RefreshTokenRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should succeed when UserId and RefreshToken are valid")]
    public void Validate_ShouldSucceed_WhenUserIdAndRefreshTokenAreValid()
    {
        // Arrange
        var request = RefreshTokenRequestFactory.Create();

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Validate should fail with UserIdRequired when UserId is null")]
    public void Validate_ShouldFailWithUserIdRequired_WhenUserIdIsNull()
    {
        // Arrange
#nullable disable
        var request = new RefreshTokenRequest { UserId = null, RefreshToken = RefreshTokenRequestFactory.ValidRefreshToken };
#nullable enable

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(RefreshTokenRequest.UserId)
            && e.ErrorCode == "RefreshTokenRequest.UserIdRequired"
            && e.ErrorMessage == "User ID is required.");
    }

    [Theory(DisplayName = "Validate should fail with UserIdRequired when UserId is empty or whitespace")]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFailWithUserIdRequired_WhenUserIdIsEmptyOrWhitespace(string userId)
    {
        // Arrange
        var request = new RefreshTokenRequest { UserId = userId, RefreshToken = RefreshTokenRequestFactory.ValidRefreshToken };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(RefreshTokenRequest.UserId)
            && e.ErrorCode == "RefreshTokenRequest.UserIdRequired"
            && e.ErrorMessage == "User ID is required.");
    }

    [Theory(DisplayName = "Validate should fail with UserIdInvalid when UserId is not a valid ULID")]
    [InlineData("not-a-ulid")]
    [InlineData("12345")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Validate_ShouldFailWithUserIdInvalid_WhenUserIdIsNotValidUlid(string userId)
    {
        // Arrange
        var request = new RefreshTokenRequest { UserId = userId, RefreshToken = RefreshTokenRequestFactory.ValidRefreshToken };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(RefreshTokenRequest.UserId)
            && e.ErrorCode == "RefreshTokenRequest.UserIdInvalid"
            && e.ErrorMessage == "User ID must be a valid ULID.");
    }

    [Fact(DisplayName = "Validate should fail with RefreshTokenRequired when RefreshToken is null")]
    public void Validate_ShouldFailWithRefreshTokenRequired_WhenRefreshTokenIsNull()
    {
        // Arrange
#nullable disable
        var request = new RefreshTokenRequest { UserId = RefreshTokenRequestFactory.ValidUserId, RefreshToken = null };
#nullable enable

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(RefreshTokenRequest.RefreshToken)
            && e.ErrorCode == "RefreshTokenRequest.RefreshTokenRequired"
            && e.ErrorMessage == "Refresh token is required.");
    }

    [Theory(DisplayName = "Validate should fail with RefreshTokenRequired when RefreshToken is empty or whitespace")]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFailWithRefreshTokenRequired_WhenRefreshTokenIsEmptyOrWhitespace(string refreshToken)
    {
        // Arrange
        var request = new RefreshTokenRequest { UserId = RefreshTokenRequestFactory.ValidUserId, RefreshToken = refreshToken };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(RefreshTokenRequest.RefreshToken)
            && e.ErrorCode == "RefreshTokenRequest.RefreshTokenRequired"
            && e.ErrorMessage == "Refresh token is required.");
    }
}