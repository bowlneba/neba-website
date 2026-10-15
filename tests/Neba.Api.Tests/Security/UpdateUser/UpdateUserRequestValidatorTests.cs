using Neba.Api.Contracts.Security;
using Neba.Api.Security.UpdateUser;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Security.UpdateUser;

[UnitTest]
[Component("Security")]
public sealed class UpdateUserRequestValidatorTests
{
    private readonly UpdateUserRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should succeed for a valid payload")]
    public void Validate_ShouldSucceed_WhenPayloadIsValid()
    {
        // Arrange
        var request = UpdateUserRequestFactory.Create();

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact(DisplayName = "Validate should succeed when both sets are empty so overrides can be cleared")]
    public void Validate_ShouldSucceed_WhenBothSetsAreEmpty()
    {
        // Arrange
        var request = UpdateUserRequestFactory.Create(addedPermissions: [], removedPermissions: []);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact(DisplayName = "Validate should fail with UserIdInvalid when the user ID is not a ULID")]
    public void Validate_ShouldFailWithUserIdInvalid_WhenUserIdIsNotUlid()
    {
        // Arrange
        var request = UpdateUserRequestFactory.Create(userId: "nope");

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "UpdateUserRequest.UserIdInvalid");
    }

    [Fact(DisplayName = "Validate should fail with AddedPermissionUnknown when an added permission is not recognized")]
    public void Validate_ShouldFailWithAddedPermissionUnknown()
    {
        // Arrange
        var request = UpdateUserRequestFactory.Create(addedPermissions: ["Not.Real"], removedPermissions: []);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "UpdateUserRequest.AddedPermissionUnknown");
    }

    [Fact(DisplayName = "Validate should fail with RemovedPermissionUnknown when a removed permission is not recognized")]
    public void Validate_ShouldFailWithRemovedPermissionUnknown()
    {
        // Arrange
        var request = UpdateUserRequestFactory.Create(addedPermissions: [], removedPermissions: ["Not.Real"]);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "UpdateUserRequest.RemovedPermissionUnknown");
    }

    [Fact(DisplayName = "Validate should fail with AddedPermissionDuplicate when an added permission is repeated")]
    public void Validate_ShouldFailWithAddedPermissionDuplicate()
    {
        // Arrange
        var request = UpdateUserRequestFactory.Create(
            addedPermissions: [Permissions.CreateArticle.Value, Permissions.CreateArticle.Value],
            removedPermissions: []);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "UpdateUserRequest.AddedPermissionDuplicate");
    }

    [Fact(DisplayName = "Validate should fail with RemovedPermissionDuplicate when a removed permission is repeated")]
    public void Validate_ShouldFailWithRemovedPermissionDuplicate()
    {
        // Arrange
        var request = UpdateUserRequestFactory.Create(
            addedPermissions: [],
            removedPermissions: [Permissions.CreateArticle.Value, Permissions.CreateArticle.Value]);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "UpdateUserRequest.RemovedPermissionDuplicate");
    }

    [Fact(DisplayName = "Validate should fail with PermissionAddedAndRemoved when a permission is in both sets")]
    public void Validate_ShouldFailWithPermissionAddedAndRemoved()
    {
        // Arrange
        var request = UpdateUserRequestFactory.Create(
            addedPermissions: [Permissions.CreateArticle.Value],
            removedPermissions: [Permissions.CreateArticle.Value]);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "UpdateUserRequest.PermissionAddedAndRemoved");
    }
}