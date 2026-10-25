using Neba.Api.Contracts.Security.ForgotPassword;
using Neba.Api.Security.Password.ForgotPassword;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Security.Password.ForgotPassword;

[UnitTest]
[Component("Security")]
public sealed class ForgotPasswordRequestValidatorTests
{
    private readonly ForgotPasswordRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should succeed when the email is valid")]
    public void Validate_ShouldSucceed_WhenEmailIsValid()
    {
        // Arrange
        var request = ForgotPasswordRequestFactory.Create();

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory(DisplayName = "Validate should fail with EmailRequired when the email is empty or whitespace")]
    [InlineData("", TestDisplayName = "Empty string")]
    [InlineData("   ", TestDisplayName = "Whitespace only")]
    public void Validate_ShouldFailWithEmailRequired_WhenEmailIsEmpty(string email)
    {
        // Arrange
        var request = ForgotPasswordRequestFactory.Create(email: email);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "ForgotPasswordRequest.EmailRequired");
    }

    [Fact(DisplayName = "Validate should fail with EmailInvalid when the email is malformed")]
    public void Validate_ShouldFailWithEmailInvalid_WhenEmailIsMalformed()
    {
        // Arrange
        var request = ForgotPasswordRequestFactory.Create(email: "not-an-email");

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "ForgotPasswordRequest.EmailInvalid");
    }

    [Fact(DisplayName = "Validate should fail with EmailTooLong when the email exceeds 256 characters")]
    public void Validate_ShouldFailWithEmailTooLong_WhenEmailExceedsMaxLength()
    {
        // Arrange
        var request = new ForgotPasswordRequest { Email = new string('a', 251) + "@x.com" };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "ForgotPasswordRequest.EmailTooLong");
    }
}
