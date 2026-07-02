using System.Security.Claims;

using Microsoft.AspNetCore.Identity;

using Neba.Api.Contracts.Security;
using Neba.Api.Security.Domain;
using Neba.Api.Security.Infrastructure;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Security.Infrastructure;

[UnitTest]
[Component("Security")]
public sealed class SecurityRoleSeederTests
{
    private static Mock<RoleManager<ApplicationRole>> CreateRoleManagerMock()
    {
        var mock = new Mock<RoleManager<ApplicationRole>>(MockBehavior.Strict, Mock.Of<IRoleStore<ApplicationRole>>(), null!, null!, null!, null!);
        mock.SetupAllProperties();
        return mock;
    }

    [Fact(DisplayName = "SeedAsync should create the Admin role and add all permission claims when the role does not exist")]
    public async Task SeedAsync_ShouldCreateRoleAndAddAllPermissionClaims_WhenRoleDoesNotExist()
    {
        // Arrange
        var roleManagerMock = CreateRoleManagerMock();

        roleManagerMock
            .Setup(m => m.FindByNameAsync(Roles.Admin))
            .ReturnsAsync((ApplicationRole?)null);
        roleManagerMock
            .Setup(m => m.CreateAsync(It.Is<ApplicationRole>(r => r.Name == Roles.Admin)))
            .ReturnsAsync(IdentityResult.Success);
        roleManagerMock
            .Setup(m => m.GetClaimsAsync(It.IsAny<ApplicationRole>()))
            .ReturnsAsync([]);

        foreach (var permission in Permissions.List)
        {
            roleManagerMock
                .Setup(m => m.AddClaimAsync(
                    It.IsAny<ApplicationRole>(),
                    It.Is<Claim>(c => c.Type == SecurityRoleSeeder.PermissionClaimType && c.Value == permission.Value)))
                .ReturnsAsync(IdentityResult.Success);
        }

        // Act
        await SecurityRoleSeeder.SeedAsync(roleManagerMock.Object);

        // Assert
        roleManagerMock.VerifyAll();
    }

    [Fact(DisplayName = "SeedAsync should not create the Admin role when it already exists")]
    public async Task SeedAsync_ShouldNotCreateRole_WhenRoleAlreadyExists()
    {
        // Arrange
        var existingRole = ApplicationRoleFactory.Create(name: Roles.Admin);
        var roleManagerMock = CreateRoleManagerMock();

        roleManagerMock
            .Setup(m => m.FindByNameAsync(Roles.Admin))
            .ReturnsAsync(existingRole);
        roleManagerMock
            .Setup(m => m.GetClaimsAsync(existingRole))
            .ReturnsAsync([]);

        foreach (var permission in Permissions.List)
        {
            roleManagerMock
                .Setup(m => m.AddClaimAsync(
                    existingRole,
                    It.Is<Claim>(c => c.Type == SecurityRoleSeeder.PermissionClaimType && c.Value == permission.Value)))
                .ReturnsAsync(IdentityResult.Success);
        }

        // Act
        await SecurityRoleSeeder.SeedAsync(roleManagerMock.Object);

        // Assert
        roleManagerMock.VerifyAll();
        roleManagerMock.Verify(m => m.CreateAsync(It.IsAny<ApplicationRole>()), Times.Never);
    }

    [Fact(DisplayName = "SeedAsync should not add or remove claims when the Admin role already has exactly the expected permission claims")]
    public async Task SeedAsync_ShouldNotAddOrRemoveClaims_WhenRoleAlreadyHasExactPermissionClaims()
    {
        // Arrange
        var existingRole = ApplicationRoleFactory.Create(name: Roles.Admin);
        var existingClaims = Permissions.List
            .Select(p => new Claim(SecurityRoleSeeder.PermissionClaimType, p.Value))
            .ToList();

        var roleManagerMock = CreateRoleManagerMock();

        roleManagerMock
            .Setup(m => m.FindByNameAsync(Roles.Admin))
            .ReturnsAsync(existingRole);
        roleManagerMock
            .Setup(m => m.GetClaimsAsync(existingRole))
            .ReturnsAsync(existingClaims);

        // Act
        await SecurityRoleSeeder.SeedAsync(roleManagerMock.Object);

        // Assert
        roleManagerMock.VerifyAll();
        roleManagerMock.Verify(m => m.CreateAsync(It.IsAny<ApplicationRole>()), Times.Never);
        roleManagerMock.Verify(m => m.AddClaimAsync(It.IsAny<ApplicationRole>(), It.IsAny<Claim>()), Times.Never);
        roleManagerMock.Verify(m => m.RemoveClaimAsync(It.IsAny<ApplicationRole>(), It.IsAny<Claim>()), Times.Never);
    }

    [Fact(DisplayName = "SeedAsync should remove a stale permission claim that is no longer in the Admin role's permissions list")]
    public async Task SeedAsync_ShouldRemoveStalePermissionClaim_WhenClaimIsNotInPermissionsList()
    {
        // Arrange
        var existingRole = ApplicationRoleFactory.Create(name: Roles.Admin);
        var staleClaim = new Claim(SecurityRoleSeeder.PermissionClaimType, "Obsolete");
        var existingClaims = Permissions.List
            .Select(p => new Claim(SecurityRoleSeeder.PermissionClaimType, p.Value))
            .Append(staleClaim)
            .ToList();

        var roleManagerMock = CreateRoleManagerMock();

        roleManagerMock
            .Setup(m => m.FindByNameAsync(Roles.Admin))
            .ReturnsAsync(existingRole);
        roleManagerMock
            .Setup(m => m.GetClaimsAsync(existingRole))
            .ReturnsAsync(existingClaims);
        roleManagerMock
            .Setup(m => m.RemoveClaimAsync(existingRole, staleClaim))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await SecurityRoleSeeder.SeedAsync(roleManagerMock.Object);

        // Assert
        roleManagerMock.VerifyAll();
        roleManagerMock.Verify(m => m.AddClaimAsync(It.IsAny<ApplicationRole>(), It.IsAny<Claim>()), Times.Never);
    }

    [Fact(DisplayName = "SeedAsync should ignore existing claims whose claim type is not the permission claim type")]
    public async Task SeedAsync_ShouldIgnoreNonPermissionClaims_WhenSyncingClaims()
    {
        // Arrange
        var existingRole = ApplicationRoleFactory.Create(name: Roles.Admin);
        var unrelatedClaim = new Claim(ClaimTypes.Role, Roles.Admin);
        var existingClaims = Permissions.List
            .Select(p => new Claim(SecurityRoleSeeder.PermissionClaimType, p.Value))
            .Append(unrelatedClaim)
            .ToList();

        var roleManagerMock = CreateRoleManagerMock();

        roleManagerMock
            .Setup(m => m.FindByNameAsync(Roles.Admin))
            .ReturnsAsync(existingRole);
        roleManagerMock
            .Setup(m => m.GetClaimsAsync(existingRole))
            .ReturnsAsync(existingClaims);

        // Act
        await SecurityRoleSeeder.SeedAsync(roleManagerMock.Object);

        // Assert
        roleManagerMock.VerifyAll();
        roleManagerMock.Verify(m => m.AddClaimAsync(It.IsAny<ApplicationRole>(), It.IsAny<Claim>()), Times.Never);
        roleManagerMock.Verify(m => m.RemoveClaimAsync(It.IsAny<ApplicationRole>(), It.IsAny<Claim>()), Times.Never);
    }

    // Template for future roles: when SecurityRoleSeeder.RolePermissions grants a new role a specific
    // (non-"all permissions") subset, add one Fact for that role that sets up the same FindByNameAsync
    // (null)/CreateAsync/GetClaimsAsync (empty) scaffolding as SeedAsync_ShouldCreateRoleAndAddAllPermissionClaims_WhenRoleDoesNotExist
    // above, but iterates only that role's expected permissions when setting up AddClaimAsync, and adds
    // a roleManagerMock.Verify(...) asserting AddClaimAsync is never called with a claim value outside
    // that expected set. One Fact per role is enough — no need to re-run every scenario above per role.
}