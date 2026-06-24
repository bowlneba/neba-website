using Neba.Api.Contracts.Security.Register;
using Neba.Api.Security.Register;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Security.Register;

[UnitTest]
[Component("Security")]
public sealed class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should succeed when email and password are valid")]
    public void Validate_ShouldSucceed_WhenEmailAndPasswordAreValid()
    {
        // Arrange
        var request = RegisterRequestFactory.Create();

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
        var request = new RegisterRequest { Email = null, Password = RegisterRequestFactory.ValidPassword };
#nullable enable

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(RegisterRequest.Email)
            && e.ErrorCode == "RegisterRequest.EmailRequired"
            && e.ErrorMessage == "Email is required.");
    }

    [Theory(DisplayName = "Validate should fail with EmailRequired when email is empty or whitespace")]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFailWithEmailRequired_WhenEmailIsEmptyOrWhitespace(string email)
    {
        // Arrange
        var request = new RegisterRequest { Email = email, Password = RegisterRequestFactory.ValidPassword };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(RegisterRequest.Email)
            && e.ErrorCode == "RegisterRequest.EmailRequired"
            && e.ErrorMessage == "Email is required.");
    }

    [Theory(DisplayName = "Validate should fail with EmailInvalid when email is not a valid address")]
    [InlineData("notanemail")]
    [InlineData("missing@")]
    [InlineData("@nodomain.com")]
    public void Validate_ShouldFailWithEmailInvalid_WhenEmailIsNotValidFormat(string email)
    {
        // Arrange
        var request = new RegisterRequest { Email = email, Password = RegisterRequestFactory.ValidPassword };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(RegisterRequest.Email)
            && e.ErrorCode == "RegisterRequest.EmailInvalid"
            && e.ErrorMessage == "A valid email address is required.");
    }

    [Fact(DisplayName = "Validate should fail with PasswordRequired when password is null")]
    public void Validate_ShouldFailWithPasswordRequired_WhenPasswordIsNull()
    {
        // Arrange
#nullable disable
        var request = new RegisterRequest { Email = RegisterRequestFactory.ValidEmail, Password = null };
#nullable enable

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(RegisterRequest.Password)
            && e.ErrorCode == "RegisterRequest.PasswordRequired"
            && e.ErrorMessage == "Password is required.");
    }

    [Theory(DisplayName = "Validate should fail with PasswordRequired when password is empty or whitespace")]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFailWithPasswordRequired_WhenPasswordIsEmptyOrWhitespace(string password)
    {
        // Arrange
        var request = new RegisterRequest { Email = RegisterRequestFactory.ValidEmail, Password = password };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(RegisterRequest.Password)
            && e.ErrorCode == "RegisterRequest.PasswordRequired"
            && e.ErrorMessage == "Password is required.");
    }

    [Theory(DisplayName = "Validate should fail with PasswordTooShort when password is fewer than 8 characters")]
    [InlineData("Pass1")]
    [InlineData("Ab1cd")]
    public void Validate_ShouldFailWithPasswordTooShort_WhenPasswordIsTooShort(string password)
    {
        // Arrange
        var request = new RegisterRequest { Email = RegisterRequestFactory.ValidEmail, Password = password };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(RegisterRequest.Password)
            && e.ErrorCode == "RegisterRequest.PasswordTooShort"
            && e.ErrorMessage == "Password must be at least 8 characters.");
    }

    [Theory(DisplayName = "Validate should fail with PasswordRequiresDigit when password has no digit")]
    [InlineData("NoDigitsHere")]
    [InlineData("PasswordWithNoNumber")]
    public void Validate_ShouldFailWithPasswordRequiresDigit_WhenPasswordHasNoDigit(string password)
    {
        // Arrange
        var request = new RegisterRequest { Email = RegisterRequestFactory.ValidEmail, Password = password };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(RegisterRequest.Password)
            && e.ErrorCode == "RegisterRequest.PasswordRequiresDigit"
            && e.ErrorMessage == "Password must contain at least one digit.");
    }
}
