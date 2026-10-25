using Neba.Api.Security.Password.ChangePassword;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Security.Password.ChangePassword;

[UnitTest]
[Component("Security")]
public sealed class ChangePasswordRequestValidatorTests
{
    private readonly ChangePasswordRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should succeed when both passwords are valid and different")]
    public void Validate_ShouldSucceed_WhenRequestIsValid()
    {
        // Arrange
        var request = ChangePasswordRequestFactory.Create();

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact(DisplayName = "Validate should fail with CurrentPasswordRequired when the current password is empty")]
    public void Validate_ShouldFailWithCurrentPasswordRequired_WhenCurrentPasswordIsEmpty()
    {
        // Arrange
        var request = ChangePasswordRequestFactory.Create(currentPassword: string.Empty);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "ChangePasswordRequest.CurrentPasswordRequired");
    }

    [Fact(DisplayName = "Validate should fail with NewPasswordRequired when the new password is empty")]
    public void Validate_ShouldFailWithNewPasswordRequired_WhenNewPasswordIsEmpty()
    {
        // Arrange
        var request = ChangePasswordRequestFactory.Create(newPassword: string.Empty);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "ChangePasswordRequest.NewPasswordRequired");
    }

    [Fact(DisplayName = "Validate should fail with NewPasswordTooShort when the new password is under 8 characters")]
    public void Validate_ShouldFailWithNewPasswordTooShort_WhenNewPasswordIsShort()
    {
        // Arrange
        var request = ChangePasswordRequestFactory.Create(newPassword: "Ab1");

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "ChangePasswordRequest.NewPasswordTooShort");
    }

    [Fact(DisplayName = "Validate should fail with NewPasswordRequiresDigit when the new password has no digit")]
    public void Validate_ShouldFailWithNewPasswordRequiresDigit_WhenNewPasswordHasNoDigit()
    {
        // Arrange
        var request = ChangePasswordRequestFactory.Create(newPassword: "NoDigitsHere");

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "ChangePasswordRequest.NewPasswordRequiresDigit");
    }
}