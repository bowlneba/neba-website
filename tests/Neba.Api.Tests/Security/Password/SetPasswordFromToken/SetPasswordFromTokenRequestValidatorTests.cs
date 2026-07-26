using Neba.Api.Contracts.Security.SetPasswordFromToken;
using Neba.Api.Security.Password.SetPasswordFromToken;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Security.Password.SetPasswordFromToken;

[UnitTest]
[Component("Security")]
public sealed class SetPasswordFromTokenRequestValidatorTests
{
    private readonly SetPasswordFromTokenRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should succeed when request is valid")]
    public void Validate_ShouldSucceed_WhenRequestIsValid()
    {
        // Arrange
        var request = SetPasswordFromTokenRequestFactory.Create();

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
        var request = new SetPasswordFromTokenRequest
        {
            UserId = null,
            Token = SetPasswordFromTokenRequestFactory.ValidToken,
            NewPassword = SetPasswordFromTokenRequestFactory.ValidNewPassword
        };
#nullable enable

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(SetPasswordFromTokenRequest.UserId)
            && e.ErrorCode == "SetPasswordFromTokenRequest.UserIdRequired"
            && e.ErrorMessage == "User ID is required.");
    }

    [Theory(DisplayName = "Validate should fail with UserIdRequired when user ID is empty or whitespace")]
    [InlineData("", TestDisplayName = "Empty string")]
    [InlineData("   ", TestDisplayName = "Whitespace only")]
    public void Validate_ShouldFailWithUserIdRequired_WhenUserIdIsEmptyOrWhitespace(string userId)
    {
        // Arrange
        var request = SetPasswordFromTokenRequestFactory.Create(userId: userId);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(SetPasswordFromTokenRequest.UserId)
            && e.ErrorCode == "SetPasswordFromTokenRequest.UserIdRequired"
            && e.ErrorMessage == "User ID is required.");
    }

    [Theory(DisplayName = "Validate should fail with UserIdInvalid when user ID is not a valid ULID")]
    [InlineData("not-a-ulid", TestDisplayName = "Not a Ulid")]
    [InlineData("12345", TestDisplayName = "Too short")]
    [InlineData("00000000-0000-0000-0000-000000000000", TestDisplayName = "GUID format, not Ulid")]
    public void Validate_ShouldFailWithUserIdInvalid_WhenUserIdIsNotAValidUlid(string userId)
    {
        // Arrange
        var request = SetPasswordFromTokenRequestFactory.Create(userId: userId);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(SetPasswordFromTokenRequest.UserId)
            && e.ErrorCode == "SetPasswordFromTokenRequest.UserIdInvalid"
            && e.ErrorMessage == "User ID must be a valid ULID.");
    }

    [Fact(DisplayName = "Validate should fail with TokenRequired when token is null")]
    public void Validate_ShouldFailWithTokenRequired_WhenTokenIsNull()
    {
        // Arrange
#nullable disable
        var request = new SetPasswordFromTokenRequest
        {
            UserId = SetPasswordFromTokenRequestFactory.ValidUserId,
            Token = null,
            NewPassword = SetPasswordFromTokenRequestFactory.ValidNewPassword
        };
#nullable enable

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(SetPasswordFromTokenRequest.Token)
            && e.ErrorCode == "SetPasswordFromTokenRequest.TokenRequired"
            && e.ErrorMessage == "Token is required.");
    }

    [Theory(DisplayName = "Validate should fail with TokenRequired when token is empty or whitespace")]
    [InlineData("", TestDisplayName = "Empty string")]
    [InlineData("   ", TestDisplayName = "Whitespace only")]
    public void Validate_ShouldFailWithTokenRequired_WhenTokenIsEmptyOrWhitespace(string token)
    {
        // Arrange
        var request = SetPasswordFromTokenRequestFactory.Create(token: token);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(SetPasswordFromTokenRequest.Token)
            && e.ErrorCode == "SetPasswordFromTokenRequest.TokenRequired"
            && e.ErrorMessage == "Token is required.");
    }

    [Fact(DisplayName = "Validate should fail with NewPasswordRequired when new password is null")]
    public void Validate_ShouldFailWithNewPasswordRequired_WhenNewPasswordIsNull()
    {
        // Arrange
#nullable disable
        var request = new SetPasswordFromTokenRequest
        {
            UserId = SetPasswordFromTokenRequestFactory.ValidUserId,
            Token = SetPasswordFromTokenRequestFactory.ValidToken,
            NewPassword = null
        };
#nullable enable

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(SetPasswordFromTokenRequest.NewPassword)
            && e.ErrorCode == "SetPasswordFromTokenRequest.NewPasswordRequired"
            && e.ErrorMessage == "Password is required.");
    }

    [Theory(DisplayName = "Validate should fail with NewPasswordRequired when new password is empty or whitespace")]
    [InlineData("", TestDisplayName = "Empty string")]
    [InlineData("   ", TestDisplayName = "Whitespace only")]
    public void Validate_ShouldFailWithNewPasswordRequired_WhenNewPasswordIsEmptyOrWhitespace(string newPassword)
    {
        // Arrange
        var request = SetPasswordFromTokenRequestFactory.Create(newPassword: newPassword);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(SetPasswordFromTokenRequest.NewPassword)
            && e.ErrorCode == "SetPasswordFromTokenRequest.NewPasswordRequired"
            && e.ErrorMessage == "Password is required.");
    }

    [Fact(DisplayName = "Validate should fail with NewPasswordTooShort when new password is under 8 characters")]
    public void Validate_ShouldFailWithNewPasswordTooShort_WhenNewPasswordIsUnder8Characters()
    {
        // Arrange
        var request = SetPasswordFromTokenRequestFactory.Create(newPassword: "Ab1");

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(SetPasswordFromTokenRequest.NewPassword)
            && e.ErrorCode == "SetPasswordFromTokenRequest.NewPasswordTooShort"
            && e.ErrorMessage == "Password must be at least 8 characters.");
    }

    [Fact(DisplayName = "Validate should fail with NewPasswordRequiresDigit when new password has no digit")]
    public void Validate_ShouldFailWithNewPasswordRequiresDigit_WhenNewPasswordHasNoDigit()
    {
        // Arrange
        var request = SetPasswordFromTokenRequestFactory.Create(newPassword: "NoDigitsHere");

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(SetPasswordFromTokenRequest.NewPassword)
            && e.ErrorCode == "SetPasswordFromTokenRequest.NewPasswordRequiresDigit"
            && e.ErrorMessage == "Password must contain at least one digit.");
    }
}
