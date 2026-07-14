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

    // SeedAsync loops over every role in SecurityRoleSeeder.RolePermissions in a single call, so any test
    // exercising one role must also give the strict mock a no-op setup (role already exists with exactly
    // its expected claims) for every other role, or that role's FindByNameAsync call throws.
    private static void SetupRoleAlreadySynced(Mock<RoleManager<ApplicationRole>> mock, string roleName, IReadOnlyCollection<Permissions> permissions)
    {
        var role = ApplicationRoleFactory.Create(name: roleName);
        var claims = permissions
            .Select(p => new Claim(SecurityRoleSeeder.PermissionClaimType, p.Value))
            .ToList();

        mock.Setup(m => m.FindByNameAsync(roleName)).ReturnsAsync(role);
        mock.Setup(m => m.GetClaimsAsync(role)).ReturnsAsync(claims);
    }

    private static void SetupOtherRolesAlreadySynced(Mock<RoleManager<ApplicationRole>> mock, string roleUnderTest)
    {
        if (roleUnderTest != Roles.Admin)
        {
            SetupRoleAlreadySynced(mock, Roles.Admin, Permissions.List);
        }

        if (roleUnderTest != Roles.Webmaster)
        {
            SetupRoleAlreadySynced(mock, Roles.Webmaster, [Permissions.CreateArticle, Permissions.DeleteArticle]);
        }

        if (roleUnderTest != Roles.Member)
        {
            SetupRoleAlreadySynced(mock, Roles.Member, []);
        }
    }

    [Fact(DisplayName = "SeedAsync should create the Admin role and add all permission claims when the role does not exist")]
    public async Task SeedAsync_ShouldCreateRoleAndAddAllPermissionClaims_WhenRoleDoesNotExist()
    {
        // Arrange
        var roleManagerMock = CreateRoleManagerMock();
        SetupOtherRolesAlreadySynced(roleManagerMock, Roles.Admin);

        roleManagerMock
            .Setup(m => m.FindByNameAsync(Roles.Admin))
            .ReturnsAsync((ApplicationRole?)null);
        roleManagerMock
            .Setup(m => m.CreateAsync(It.Is<ApplicationRole>(r => r.Name == Roles.Admin)))
            .ReturnsAsync(IdentityResult.Success);
        roleManagerMock
            .Setup(m => m.GetClaimsAsync(It.Is<ApplicationRole>(r => r.Name == Roles.Admin)))
            .ReturnsAsync([]);

        foreach (var permission in Permissions.List)
        {
            roleManagerMock
                .Setup(m => m.AddClaimAsync(
                    It.Is<ApplicationRole>(r => r.Name == Roles.Admin),
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
        SetupOtherRolesAlreadySynced(roleManagerMock, Roles.Admin);

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
        SetupOtherRolesAlreadySynced(roleManagerMock, Roles.Admin);

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
        roleManagerMock.Verify(m => m.AddClaimAsync(existingRole, It.IsAny<Claim>()), Times.Never);
        roleManagerMock.Verify(m => m.RemoveClaimAsync(existingRole, It.IsAny<Claim>()), Times.Never);
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
        SetupOtherRolesAlreadySynced(roleManagerMock, Roles.Admin);

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
        roleManagerMock.Verify(m => m.AddClaimAsync(existingRole, It.IsAny<Claim>()), Times.Never);
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
        SetupOtherRolesAlreadySynced(roleManagerMock, Roles.Admin);

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
        roleManagerMock.Verify(m => m.AddClaimAsync(existingRole, It.IsAny<Claim>()), Times.Never);
        roleManagerMock.Verify(m => m.RemoveClaimAsync(existingRole, It.IsAny<Claim>()), Times.Never);
    }

    [Fact(DisplayName = "SeedAsync should create the Webmaster role and add exactly the CreateArticle and DeleteArticle permission claims when the role does not exist")]
    public async Task SeedAsync_ShouldCreateWebmasterRoleAndAddCreateAndDeleteArticleClaims_WhenRoleDoesNotExist()
    {
        // Arrange
        var roleManagerMock = CreateRoleManagerMock();
        SetupOtherRolesAlreadySynced(roleManagerMock, Roles.Webmaster);

        roleManagerMock
            .Setup(m => m.FindByNameAsync(Roles.Webmaster))
            .ReturnsAsync((ApplicationRole?)null);
        roleManagerMock
            .Setup(m => m.CreateAsync(It.Is<ApplicationRole>(r => r.Name == Roles.Webmaster)))
            .ReturnsAsync(IdentityResult.Success);
        roleManagerMock
            .Setup(m => m.GetClaimsAsync(It.Is<ApplicationRole>(r => r.Name == Roles.Webmaster)))
            .ReturnsAsync([]);
        roleManagerMock
            .Setup(m => m.AddClaimAsync(
                It.Is<ApplicationRole>(r => r.Name == Roles.Webmaster),
                It.Is<Claim>(c => c.Type == SecurityRoleSeeder.PermissionClaimType && c.Value == Permissions.CreateArticle.Value)))
            .ReturnsAsync(IdentityResult.Success);
        roleManagerMock
            .Setup(m => m.AddClaimAsync(
                It.Is<ApplicationRole>(r => r.Name == Roles.Webmaster),
                It.Is<Claim>(c => c.Type == SecurityRoleSeeder.PermissionClaimType && c.Value == Permissions.DeleteArticle.Value)))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await SecurityRoleSeeder.SeedAsync(roleManagerMock.Object);

        // Assert
        roleManagerMock.VerifyAll();
        roleManagerMock.Verify(
            m => m.AddClaimAsync(
                It.Is<ApplicationRole>(r => r.Name == Roles.Webmaster),
                It.Is<Claim>(c => c.Type == SecurityRoleSeeder.PermissionClaimType
                    && c.Value != Permissions.CreateArticle.Value
                    && c.Value != Permissions.DeleteArticle.Value)),
            Times.Never);
    }

    [Fact(DisplayName = "SeedAsync should create the Member role and add no permission claims when the role does not exist")]
    public async Task SeedAsync_ShouldCreateMemberRoleAndAddNoPermissionClaims_WhenRoleDoesNotExist()
    {
        // Arrange
        var roleManagerMock = CreateRoleManagerMock();
        SetupOtherRolesAlreadySynced(roleManagerMock, Roles.Member);

        roleManagerMock
            .Setup(m => m.FindByNameAsync(Roles.Member))
            .ReturnsAsync((ApplicationRole?)null);
        roleManagerMock
            .Setup(m => m.CreateAsync(It.Is<ApplicationRole>(r => r.Name == Roles.Member)))
            .ReturnsAsync(IdentityResult.Success);
        roleManagerMock
            .Setup(m => m.GetClaimsAsync(It.Is<ApplicationRole>(r => r.Name == Roles.Member)))
            .ReturnsAsync([]);

        // Act
        await SecurityRoleSeeder.SeedAsync(roleManagerMock.Object);

        // Assert
        roleManagerMock.VerifyAll();
        roleManagerMock.Verify(m => m.AddClaimAsync(It.Is<ApplicationRole>(r => r.Name == Roles.Member), It.IsAny<Claim>()), Times.Never);
    }
}