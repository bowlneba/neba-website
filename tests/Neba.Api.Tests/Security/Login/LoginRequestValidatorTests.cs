using Neba.Api.Contracts.Security.Login;
using Neba.Api.Security.Login;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Security.Login;

[UnitTest]
[Component("Security")]
public sealed class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should succeed when email and password are valid")]
    public void Validate_ShouldSucceed_WhenEmailAndPasswordAreValid()
    {
        // Arrange
        var request = LoginRequestFactory.Create();

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Validate should fail with EmailRequired when email is null")]
    public void Validate_ShouldFailWithEmailRequired_WhenEmailIsNull()
    {
        // Arrange
#nullable disable
        var request = new LoginRequest { Email = null, Password = LoginRequestFactory.ValidPassword };
#nullable enable

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(LoginRequest.Email)
            && e.ErrorCode == "LoginRequest.EmailRequired"
            && e.ErrorMessage == "Email is required.");
    }

    [Theory(DisplayName = "Validate should fail with EmailRequired when email is empty or whitespace")]
    [InlineData("", TestDisplayName = "Empty string")]
    [InlineData("   ", TestDisplayName = "Whitespace only")]
    public void Validate_ShouldFailWithEmailRequired_WhenEmailIsEmptyOrWhitespace(string email)
    {
        // Arrange
        var request = new LoginRequest { Email = email, Password = LoginRequestFactory.ValidPassword };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(LoginRequest.Email)
            && e.ErrorCode == "LoginRequest.EmailRequired"
            && e.ErrorMessage == "Email is required.");
    }

    [Theory(DisplayName = "Validate should fail with EmailInvalid when email is not a valid address")]
    [InlineData("notanemail", TestDisplayName = "Missing @ and domain")]
    [InlineData("missing@", TestDisplayName = "Missing domain")]
    [InlineData("@nodomain.com", TestDisplayName = "Missing local part")]
    public void Validate_ShouldFailWithEmailInvalid_WhenEmailIsNotValidFormat(string email)
    {
        // Arrange
        var request = new LoginRequest { Email = email, Password = LoginRequestFactory.ValidPassword };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(LoginRequest.Email)
            && e.ErrorCode == "LoginRequest.EmailInvalid"
            && e.ErrorMessage == "A valid email address is required.");
    }

    [Fact(DisplayName = "Validate should fail with PasswordRequired when password is null")]
    public void Validate_ShouldFailWithPasswordRequired_WhenPasswordIsNull()
    {
        // Arrange
#nullable disable
        var request = new LoginRequest { Email = LoginRequestFactory.ValidEmail, Password = null };
#nullable enable

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(LoginRequest.Password)
            && e.ErrorCode == "LoginRequest.PasswordRequired"
            && e.ErrorMessage == "Password is required.");
    }

    [Theory(DisplayName = "Validate should fail with PasswordRequired when password is empty or whitespace")]
    [InlineData("", TestDisplayName = "Empty string")]
    [InlineData("   ", TestDisplayName = "Whitespace only")]
    public void Validate_ShouldFailWithPasswordRequired_WhenPasswordIsEmptyOrWhitespace(string password)
    {
        // Arrange
        var request = new LoginRequest { Email = LoginRequestFactory.ValidEmail, Password = password };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(LoginRequest.Password)
            && e.ErrorCode == "LoginRequest.PasswordRequired"
            && e.ErrorMessage == "Password is required.");
    }
}