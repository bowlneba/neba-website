using System.Security.Claims;

using Microsoft.AspNetCore.Identity;

using Neba.Api.Contracts.Security;
using Neba.Api.Security.Domain;
using Neba.Api.Security.Infrastructure;
using Neba.Api.Security.Infrastructure.Authorization;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Security.Infrastructure.Authorization;

[UnitTest]
[Component("Security")]
public sealed class PermissionResolverTests
{
    private static Mock<RoleManager<ApplicationRole>> CreateRoleManagerMock()
    {
        var mock = new Mock<RoleManager<ApplicationRole>>(MockBehavior.Strict, Mock.Of<IRoleStore<ApplicationRole>>(), null!, null!, null!, null!);
        mock.SetupAllProperties();
        return mock;
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var mock = new Mock<UserManager<ApplicationUser>>(
            MockBehavior.Strict, Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        mock.SetupAllProperties();
        return mock;
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock(ApplicationUser user, params Claim[] storedClaims)
    {
        var mock = CreateUserManagerMock();
        mock.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync(storedClaims);
        return mock;
    }

    [Fact(DisplayName = "ResolveAsync should return an empty collection when no role names are provided")]
    public async Task ResolveAsync_ShouldReturnEmptyCollection_WhenNoRoleNamesProvided()
    {
        // Arrange
        var roleManagerMock = CreateRoleManagerMock();
        var user = ApplicationUserFactory.Create();
        var userManagerMock = CreateUserManagerMock(user);

        // Act
        var result = await PermissionResolver.ResolveAsync(roleManagerMock.Object, userManagerMock.Object, user, []);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "ResolveAsync should skip a role name that does not resolve to a role")]
    public async Task ResolveAsync_ShouldSkipRole_WhenRoleNotFound()
    {
        // Arrange
        var roleManagerMock = CreateRoleManagerMock();
        roleManagerMock
            .Setup(m => m.FindByNameAsync(Roles.Admin))
            .ReturnsAsync((ApplicationRole?)null);

        var user = ApplicationUserFactory.Create();
        var userManagerMock = CreateUserManagerMock(user);

        // Act
        var result = await PermissionResolver.ResolveAsync(roleManagerMock.Object, userManagerMock.Object, user, [Roles.Admin]);

        // Assert
        result.ShouldBeEmpty();
        roleManagerMock.Verify(m => m.GetClaimsAsync(It.IsAny<ApplicationRole>()), Times.Never);
    }

    [Fact(DisplayName = "ResolveAsync should return the permission claim values for a resolved role")]
    public async Task ResolveAsync_ShouldReturnPermissionClaimValues_WhenRoleResolves()
    {
        // Arrange
        var role = ApplicationRoleFactory.Create(name: Roles.Admin);
        var roleManagerMock = CreateRoleManagerMock();
        roleManagerMock
            .Setup(m => m.FindByNameAsync(Roles.Admin))
            .ReturnsAsync(role);
        roleManagerMock
            .Setup(m => m.GetClaimsAsync(role))
            .ReturnsAsync([new Claim(SecurityRoleSeeder.PermissionClaimType, Permissions.CreateArticle.Value)]);

        var user = ApplicationUserFactory.Create();
        var userManagerMock = CreateUserManagerMock(user);

        // Act
        var result = await PermissionResolver.ResolveAsync(roleManagerMock.Object, userManagerMock.Object, user, [Roles.Admin]);

        // Assert
        result.ShouldBe([Permissions.CreateArticle]);
    }

    [Fact(DisplayName = "ResolveAsync should ignore claims whose claim type is not the permission claim type")]
    public async Task ResolveAsync_ShouldIgnoreNonPermissionClaims()
    {
        // Arrange
        var role = ApplicationRoleFactory.Create(name: Roles.Admin);
        var roleManagerMock = CreateRoleManagerMock();
        roleManagerMock
            .Setup(m => m.FindByNameAsync(Roles.Admin))
            .ReturnsAsync(role);
        roleManagerMock
            .Setup(m => m.GetClaimsAsync(role))
            .ReturnsAsync([new Claim(ClaimTypes.Role, Roles.Admin)]);

        var user = ApplicationUserFactory.Create();
        var userManagerMock = CreateUserManagerMock(user);

        // Act
        var result = await PermissionResolver.ResolveAsync(roleManagerMock.Object, userManagerMock.Object, user, [Roles.Admin]);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "ResolveAsync should deduplicate permission keys shared across multiple roles")]
    public async Task ResolveAsync_ShouldDeduplicatePermissionKeys_AcrossMultipleRoles()
    {
        // Arrange
        var adminRole = ApplicationRoleFactory.Create(name: Roles.Admin);
        const string memberRoleName = "Member";
        var memberRole = ApplicationRoleFactory.Create(name: memberRoleName);
        var roleManagerMock = CreateRoleManagerMock();
        roleManagerMock
            .Setup(m => m.FindByNameAsync(Roles.Admin))
            .ReturnsAsync(adminRole);
        roleManagerMock
            .Setup(m => m.GetClaimsAsync(adminRole))
            .ReturnsAsync([new Claim(SecurityRoleSeeder.PermissionClaimType, "News.CreateArticle")]);
        roleManagerMock
            .Setup(m => m.FindByNameAsync(memberRoleName))
            .ReturnsAsync(memberRole);
        roleManagerMock
            .Setup(m => m.GetClaimsAsync(memberRole))
            .ReturnsAsync([new Claim(SecurityRoleSeeder.PermissionClaimType, "News.CreateArticle")]);

        var user = ApplicationUserFactory.Create();
        var userManagerMock = CreateUserManagerMock(user);

        // Act
        var result = await PermissionResolver.ResolveAsync(roleManagerMock.Object, userManagerMock.Object, user, [Roles.Admin, memberRoleName]);

        // Assert
        result.ShouldBe([Permissions.CreateArticle]);
    }

    [Fact(DisplayName = "ResolveAsync should aggregate distinct permission keys across multiple roles")]
    public async Task ResolveAsync_ShouldAggregateDistinctPermissionKeys_AcrossMultipleRoles()
    {
        // Arrange
        var adminRole = ApplicationRoleFactory.Create(name: Roles.Admin);
        const string memberRoleName = "Member";
        var memberRole = ApplicationRoleFactory.Create(name: memberRoleName);
        var roleManagerMock = CreateRoleManagerMock();
        roleManagerMock
            .Setup(m => m.FindByNameAsync(Roles.Admin))
            .ReturnsAsync(adminRole);
        roleManagerMock
            .Setup(m => m.GetClaimsAsync(adminRole))
            .ReturnsAsync([new Claim(SecurityRoleSeeder.PermissionClaimType, "News.CreateArticle")]);
        roleManagerMock
            .Setup(m => m.FindByNameAsync(memberRoleName))
            .ReturnsAsync(memberRole);
        roleManagerMock
            .Setup(m => m.GetClaimsAsync(memberRole))
            .ReturnsAsync([new Claim(SecurityRoleSeeder.PermissionClaimType, "News.EditArticle")]);

        var user = ApplicationUserFactory.Create();
        var userManagerMock = CreateUserManagerMock(user);

        // Act
        var result = await PermissionResolver.ResolveAsync(roleManagerMock.Object, userManagerMock.Object, user, [Roles.Admin, memberRoleName]);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(Permissions.CreateArticle);
        result.ShouldContain(Permissions.EditArticle);
    }

    private static Mock<RoleManager<ApplicationRole>> CreateRoleManagerWithRole(string roleName, params string[] permissionValues)
    {
        var role = ApplicationRoleFactory.Create(name: roleName);
        var mock = CreateRoleManagerMock();
        mock.Setup(m => m.FindByNameAsync(roleName)).ReturnsAsync(role);
        mock.Setup(m => m.GetClaimsAsync(role))
            .ReturnsAsync([.. permissionValues.Select(value => new Claim(SecurityRoleSeeder.PermissionClaimType, value))]);
        return mock;
    }

    [Fact(DisplayName = "ResolveAsync should add a permission the user has been granted outside their roles")]
    public async Task ResolveAsync_ShouldAddUserAddedPermission()
    {
        // Arrange
        var roleManagerMock = CreateRoleManagerWithRole(Roles.Journalist, Permissions.CreateArticle.Value);
        var user = ApplicationUserFactory.Create();
        var userManagerMock = CreateUserManagerMock(user, new Claim(Permissions.ClaimType, Permissions.CreateSponsor.Value));

        // Act
        var result = await PermissionResolver.ResolveAsync(roleManagerMock.Object, userManagerMock.Object, user, [Roles.Journalist]);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(Permissions.CreateArticle);
        result.ShouldContain(Permissions.CreateSponsor);
    }

    [Fact(DisplayName = "ResolveAsync should remove a role permission the user has been denied")]
    public async Task ResolveAsync_ShouldRemoveUserDeniedPermission()
    {
        // Arrange
        var roleManagerMock = CreateRoleManagerWithRole(
            Roles.Journalist, Permissions.CreateArticle.Value, Permissions.EditArticle.Value);
        var user = ApplicationUserFactory.Create();
        var userManagerMock = CreateUserManagerMock(user, new Claim(Permissions.DeniedClaimType, Permissions.CreateArticle.Value));

        // Act
        var result = await PermissionResolver.ResolveAsync(roleManagerMock.Object, userManagerMock.Object, user, [Roles.Journalist]);

        // Assert
        result.ShouldBe([Permissions.EditArticle]);
    }

    [Fact(DisplayName = "ResolveAsync should ignore a stored denial for a user who holds the Admin role")]
    public async Task ResolveAsync_ShouldIgnoreStoredDenial_WhenUserIsAdmin()
    {
        // Arrange
        var roleManagerMock = CreateRoleManagerWithRole(Roles.Admin, Permissions.CreateArticle.Value);
        var user = ApplicationUserFactory.Create();
        var userManagerMock = CreateUserManagerMock(user, new Claim(Permissions.DeniedClaimType, Permissions.CreateArticle.Value));

        // Act
        var result = await PermissionResolver.ResolveAsync(roleManagerMock.Object, userManagerMock.Object, user, [Roles.Admin]);

        // Assert
        result.ShouldBe([Permissions.CreateArticle]);
    }

    [Fact(DisplayName = "ResolveAsync should skip a stored override whose value is no longer a known permission")]
    public async Task ResolveAsync_ShouldSkipUnknownStoredValue()
    {
        // Arrange
        var roleManagerMock = CreateRoleManagerWithRole(Roles.Journalist, Permissions.CreateArticle.Value);
        var user = ApplicationUserFactory.Create();
        var userManagerMock = CreateUserManagerMock(user, new Claim(Permissions.ClaimType, "Retired.Permission"));

        // Act
        var result = await PermissionResolver.ResolveAsync(roleManagerMock.Object, userManagerMock.Object, user, [Roles.Journalist]);

        // Assert
        result.ShouldBe([Permissions.CreateArticle]);
    }

    [Fact(DisplayName = "ResolveAsync should let a failed claim read propagate instead of falling back to role-only permissions")]
    public async Task ResolveAsync_ShouldPropagate_WhenClaimReadFails()
    {
        // Arrange
        var roleManagerMock = CreateRoleManagerWithRole(Roles.Journalist, Permissions.CreateArticle.Value);
        var user = ApplicationUserFactory.Create();
        var userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(m => m.GetClaimsAsync(user)).ThrowsAsync(new InvalidOperationException("db down"));

        // Act
        var act = () => PermissionResolver.ResolveAsync(roleManagerMock.Object, userManagerMock.Object, user, [Roles.Journalist]);

        // Assert
        await Should.ThrowAsync<InvalidOperationException>(act);
    }
}