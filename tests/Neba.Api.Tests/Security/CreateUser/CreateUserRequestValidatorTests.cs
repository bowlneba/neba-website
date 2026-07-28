using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Security.CreateUser;
using Neba.Api.Security.CreateUser;
using Neba.Api.Security.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Security.CreateUser;

[UnitTest]
[Component("Security")]
public sealed class CreateUserRequestValidatorTests
{
    private readonly CreateUserRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should succeed when email and roles are valid")]
    public void Validate_ShouldSucceed_WhenEmailAndRolesAreValid()
    {
        // Arrange
        var request = CreateUserRequestFactory.Create();

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
        var request = new CreateUserRequest { User = new CreateUserInput { Email = null, Roles = [Roles.Webmaster] } };
#nullable enable

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == $"User.{nameof(CreateUserInput.Email)}"
            && e.ErrorCode == "CreateUserRequest.EmailRequired"
            && e.ErrorMessage == "Email is required.");
    }

    [Theory(DisplayName = "Validate should fail with EmailRequired when email is empty or whitespace")]
    [InlineData("", TestDisplayName = "Empty string")]
    [InlineData("   ", TestDisplayName = "Whitespace only")]
    public void Validate_ShouldFailWithEmailRequired_WhenEmailIsEmptyOrWhitespace(string email)
    {
        // Arrange
        var request = CreateUserRequestFactory.Create(email: email);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == $"User.{nameof(CreateUserInput.Email)}"
            && e.ErrorCode == "CreateUserRequest.EmailRequired"
            && e.ErrorMessage == "Email is required.");
    }

    [Theory(DisplayName = "Validate should fail with EmailInvalid when email is not a valid address")]
    [InlineData("notanemail", TestDisplayName = "Missing @ and domain")]
    [InlineData("missing@", TestDisplayName = "Missing domain")]
    [InlineData("@nodomain.com", TestDisplayName = "Missing local part")]
    public void Validate_ShouldFailWithEmailInvalid_WhenEmailIsNotValidFormat(string email)
    {
        // Arrange
        var request = CreateUserRequestFactory.Create(email: email);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == $"User.{nameof(CreateUserInput.Email)}"
            && e.ErrorCode == "CreateUserRequest.EmailInvalid"
            && e.ErrorMessage == "A valid email address is required.");
    }

    [Fact(DisplayName = "Validate should fail with RolesRequired when roles collection is empty")]
    public void Validate_ShouldFailWithRolesRequired_WhenRolesIsEmpty()
    {
        // Arrange
        var request = CreateUserRequestFactory.Create(roles: []);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == $"User.{nameof(CreateUserInput.Roles)}"
            && e.ErrorCode == "CreateUserRequest.RolesRequired"
            && e.ErrorMessage == "At least one role is required.");
    }

    [Fact(DisplayName = "Validate should fail with AdminRoleNotAllowed when Admin role is requested")]
    public void Validate_ShouldFailWithAdminRoleNotAllowed_WhenAdminRoleRequested()
    {
        // Arrange
        var request = CreateUserRequestFactory.Create(roles: [Roles.Admin]);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.ErrorCode == "CreateUserRequest.AdminRoleNotAllowed"
            && e.ErrorMessage == "The Admin role cannot be granted through this endpoint.");
    }

    [Fact(DisplayName = "Validate should fail with RoleUnknown when a role is not recognized")]
    public void Validate_ShouldFailWithRoleUnknown_WhenRoleIsNotRecognized()
    {
        // Arrange
        var request = CreateUserRequestFactory.Create(roles: ["NotARole"]);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.ErrorCode == "CreateUserRequest.RoleUnknown"
            && e.ErrorMessage == "One or more roles are not recognized.");
    }

    [Fact(DisplayName = "Validate should fail with ClaimTypeUnsupported when a claim type other than 'permission' is requested")]
    public void Validate_ShouldFailWithClaimTypeUnsupported_WhenClaimTypeIsNotPermission()
    {
        // Arrange
        var request = CreateUserRequestFactory.Create(
            claims: [new ClaimInput { Type = "not-permission", Value = Permissions.CreateUser.Value }]);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateUserRequest.ClaimTypeUnsupported");
    }

    [Fact(DisplayName = "Validate should fail with ClaimValueUnknown when a claim value is not a recognized permission")]
    public void Validate_ShouldFailWithClaimValueUnknown_WhenClaimValueIsNotRecognizedPermission()
    {
        // Arrange
        var request = CreateUserRequestFactory.Create(
            claims: [new ClaimInput { Type = Permissions.ClaimType, Value = "Not.ARealPermission" }]);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateUserRequest.ClaimValueUnknown");
    }

    [Fact(DisplayName = "Validate should succeed when claims carry a known permission value")]
    public void Validate_ShouldSucceed_WhenClaimsCarryKnownPermissionValue()
    {
        // Arrange
        var request = CreateUserRequestFactory.Create(
            claims: [new ClaimInput { Type = Permissions.ClaimType, Value = Permissions.CreateUser.Value }]);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }
}