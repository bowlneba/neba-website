using System.Security.Claims;
using Neba.Api.Contracts.Security;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Contracts.Security;

[UnitTest]
[Component("Api.Contracts")]
public sealed class ClaimsPrincipalPermissionExtensionsTests
{
    [Fact(DisplayName = "HasAnyPermission should return true when the user has one of the requested permission claims")]
    public void HasAnyPermission_ShouldReturnTrue_WhenUserHasOneOfRequestedPermissionClaims()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(Permissions.ClaimType, Permissions.Read.Value),
        ]));

        // Act
        var result = user.HasAnyPermission([Permissions.Read, Permissions.Write]);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "HasAnyPermission should return true when the user has all of the requested permission claims")]
    public void HasAnyPermission_ShouldReturnTrue_WhenUserHasAllRequestedPermissionClaims()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(Permissions.ClaimType, Permissions.Read.Value),
            new Claim(Permissions.ClaimType, Permissions.Write.Value),
        ]));

        // Act
        var result = user.HasAnyPermission([Permissions.Read, Permissions.Write]);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "HasAnyPermission should return false when the user has none of the requested permission claims")]
    public void HasAnyPermission_ShouldReturnFalse_WhenUserHasNoneOfRequestedPermissionClaims()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(Permissions.ClaimType, Permissions.DeleteArticle.Value),
        ]));

        // Act
        var result = user.HasAnyPermission([Permissions.Read, Permissions.Write]);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "HasAnyPermission should return false when the user has no claims")]
    public void HasAnyPermission_ShouldReturnFalse_WhenUserHasNoClaims()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var result = user.HasAnyPermission([Permissions.Read, Permissions.Write]);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "HasAnyPermission should return false when the requested permissions collection is empty")]
    public void HasAnyPermission_ShouldReturnFalse_WhenRequestedPermissionsCollectionIsEmpty()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(Permissions.ClaimType, Permissions.Read.Value),
        ]));

        // Act
        var result = user.HasAnyPermission([]);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "HasAnyPermission should return false when the user has a claim of a different type with a matching value")]
    public void HasAnyPermission_ShouldReturnFalse_WhenUserHasMatchingValueUnderDifferentClaimType()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Role, Permissions.Read.Value),
        ]));

        // Act
        var result = user.HasAnyPermission([Permissions.Read]);

        // Assert
        result.ShouldBeFalse();
    }
}
