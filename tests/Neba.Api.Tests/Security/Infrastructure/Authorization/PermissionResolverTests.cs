using System.Security.Claims;

using Microsoft.AspNetCore.Identity;

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

    [Fact(DisplayName = "ResolveAsync should return an empty collection when no role names are provided")]
    public async Task ResolveAsync_ShouldReturnEmptyCollection_WhenNoRoleNamesProvided()
    {
        // Arrange
        var roleManagerMock = CreateRoleManagerMock();

        // Act
        var result = await PermissionResolver.ResolveAsync(roleManagerMock.Object, []);

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

        // Act
        var result = await PermissionResolver.ResolveAsync(roleManagerMock.Object, [Roles.Admin]);

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
            .ReturnsAsync([new Claim(SecurityRoleSeeder.PermissionClaimType, "Read")]);

        // Act
        var result = await PermissionResolver.ResolveAsync(roleManagerMock.Object, [Roles.Admin]);

        // Assert
        result.ShouldBe(["Read"]);
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

        // Act
        var result = await PermissionResolver.ResolveAsync(roleManagerMock.Object, [Roles.Admin]);

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
            .ReturnsAsync([new Claim(SecurityRoleSeeder.PermissionClaimType, "Read")]);
        roleManagerMock
            .Setup(m => m.FindByNameAsync(memberRoleName))
            .ReturnsAsync(memberRole);
        roleManagerMock
            .Setup(m => m.GetClaimsAsync(memberRole))
            .ReturnsAsync([new Claim(SecurityRoleSeeder.PermissionClaimType, "Read")]);

        // Act
        var result = await PermissionResolver.ResolveAsync(roleManagerMock.Object, [Roles.Admin, memberRoleName]);

        // Assert
        result.ShouldBe(["Read"]);
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
            .ReturnsAsync([new Claim(SecurityRoleSeeder.PermissionClaimType, "Read")]);
        roleManagerMock
            .Setup(m => m.FindByNameAsync(memberRoleName))
            .ReturnsAsync(memberRole);
        roleManagerMock
            .Setup(m => m.GetClaimsAsync(memberRole))
            .ReturnsAsync([new Claim(SecurityRoleSeeder.PermissionClaimType, "Write")]);

        // Act
        var result = await PermissionResolver.ResolveAsync(roleManagerMock.Object, [Roles.Admin, memberRoleName]);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain("Read");
        result.ShouldContain("Write");
    }
}
