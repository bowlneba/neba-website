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
            SetupRoleAlreadySynced(mock, Roles.Webmaster, [Permissions.CreateArticle, Permissions.EditArticle, Permissions.DeleteArticle, Permissions.CreateSponsor, Permissions.EditSponsor, Permissions.CreateTournament, Permissions.ManageTournamentSponsors]);
        }

        if (roleUnderTest != Roles.TournamentDirector)
        {
            SetupRoleAlreadySynced(mock, Roles.TournamentDirector, [Permissions.CreateTournament, Permissions.ManageTournamentSponsors]);
        }

        if (roleUnderTest != Roles.Journalist)
        {
            SetupRoleAlreadySynced(mock, Roles.Journalist, [Permissions.CreateArticle, Permissions.EditArticle, Permissions.DeleteArticle]);
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
    }

    [Fact(DisplayName = "SeedAsync should create the Webmaster role and add exactly the CreateArticle, EditArticle, DeleteArticle, CreateSponsor, EditSponsor, CreateTournament, and ManageTournamentSponsors permission claims when the role does not exist")]
    public async Task SeedAsync_ShouldCreateWebmasterRoleAndAddExpectedClaims_WhenRoleDoesNotExist()
    {
        // Arrange
        var expectedPermissions = new[]
        {
            Permissions.CreateArticle,
            Permissions.EditArticle,
            Permissions.DeleteArticle,
            Permissions.CreateSponsor,
            Permissions.EditSponsor,
            Permissions.CreateTournament,
            Permissions.ManageTournamentSponsors
        };

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

        foreach (var permission in expectedPermissions)
        {
            roleManagerMock
                .Setup(m => m.AddClaimAsync(
                    It.Is<ApplicationRole>(r => r.Name == Roles.Webmaster),
                    It.Is<Claim>(c => c.Type == SecurityRoleSeeder.PermissionClaimType && c.Value == permission.Value)))
                .ReturnsAsync(IdentityResult.Success);
        }

        // Act
        await SecurityRoleSeeder.SeedAsync(roleManagerMock.Object);

        // Assert
        roleManagerMock.VerifyAll();
    }

    [Fact(DisplayName = "SeedAsync should create the Tournament Director role and add exactly the CreateTournament and ManageTournamentSponsors permission claims when the role does not exist")]
    public async Task SeedAsync_ShouldCreateTournamentDirectorRoleAndAddExpectedClaims_WhenRoleDoesNotExist()
    {
        // Arrange
        var expectedPermissions = new[]
        {
            Permissions.CreateTournament,
            Permissions.ManageTournamentSponsors
        };

        var roleManagerMock = CreateRoleManagerMock();
        SetupOtherRolesAlreadySynced(roleManagerMock, Roles.TournamentDirector);

        roleManagerMock
            .Setup(m => m.FindByNameAsync(Roles.TournamentDirector))
            .ReturnsAsync((ApplicationRole?)null);
        roleManagerMock
            .Setup(m => m.CreateAsync(It.Is<ApplicationRole>(r => r.Name == Roles.TournamentDirector)))
            .ReturnsAsync(IdentityResult.Success);
        roleManagerMock
            .Setup(m => m.GetClaimsAsync(It.Is<ApplicationRole>(r => r.Name == Roles.TournamentDirector)))
            .ReturnsAsync([]);

        foreach (var permission in expectedPermissions)
        {
            roleManagerMock
                .Setup(m => m.AddClaimAsync(
                    It.Is<ApplicationRole>(r => r.Name == Roles.TournamentDirector),
                    It.Is<Claim>(c => c.Type == SecurityRoleSeeder.PermissionClaimType && c.Value == permission.Value)))
                .ReturnsAsync(IdentityResult.Success);
        }

        // Act
        await SecurityRoleSeeder.SeedAsync(roleManagerMock.Object);

        // Assert
        roleManagerMock.VerifyAll();
    }

    [Fact(DisplayName = "SeedAsync should remove a stale permission claim that is no longer in the Tournament Director role's permissions list")]
    public async Task SeedAsync_ShouldRemoveStalePermissionClaim_WhenClaimIsNotInTournamentDirectorPermissionsList()
    {
        // Arrange
        var existingRole = ApplicationRoleFactory.Create(name: Roles.TournamentDirector);
        var staleClaim = new Claim(SecurityRoleSeeder.PermissionClaimType, Permissions.EditSponsor.Value);
        var existingClaims = new[] { Permissions.CreateTournament, Permissions.ManageTournamentSponsors }
            .Select(p => new Claim(SecurityRoleSeeder.PermissionClaimType, p.Value))
            .Append(staleClaim)
            .ToList();

        var roleManagerMock = CreateRoleManagerMock();
        SetupOtherRolesAlreadySynced(roleManagerMock, Roles.TournamentDirector);

        roleManagerMock
            .Setup(m => m.FindByNameAsync(Roles.TournamentDirector))
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
    }

    [Fact(DisplayName = "SeedAsync should create the Journalist role and add exactly the CreateArticle, EditArticle, and DeleteArticle permission claims when the role does not exist")]
    public async Task SeedAsync_ShouldCreateJournalistRoleAndAddExpectedClaims_WhenRoleDoesNotExist()
    {
        // Arrange
        var expectedPermissions = new[]
        {
            Permissions.CreateArticle,
            Permissions.EditArticle,
            Permissions.DeleteArticle
        };

        var roleManagerMock = CreateRoleManagerMock();
        SetupOtherRolesAlreadySynced(roleManagerMock, Roles.Journalist);

        roleManagerMock
            .Setup(m => m.FindByNameAsync(Roles.Journalist))
            .ReturnsAsync((ApplicationRole?)null);
        roleManagerMock
            .Setup(m => m.CreateAsync(It.Is<ApplicationRole>(r => r.Name == Roles.Journalist)))
            .ReturnsAsync(IdentityResult.Success);
        roleManagerMock
            .Setup(m => m.GetClaimsAsync(It.Is<ApplicationRole>(r => r.Name == Roles.Journalist)))
            .ReturnsAsync([]);

        foreach (var permission in expectedPermissions)
        {
            roleManagerMock
                .Setup(m => m.AddClaimAsync(
                    It.Is<ApplicationRole>(r => r.Name == Roles.Journalist),
                    It.Is<Claim>(c => c.Type == SecurityRoleSeeder.PermissionClaimType && c.Value == permission.Value)))
                .ReturnsAsync(IdentityResult.Success);
        }

        // Act
        await SecurityRoleSeeder.SeedAsync(roleManagerMock.Object);

        // Assert
        roleManagerMock.VerifyAll();
    }

    [Fact(DisplayName = "SeedAsync should remove a stale permission claim that is no longer in the Journalist role's permissions list")]
    public async Task SeedAsync_ShouldRemoveStalePermissionClaim_WhenClaimIsNotInJournalistPermissionsList()
    {
        // Arrange
        var existingRole = ApplicationRoleFactory.Create(name: Roles.Journalist);
        var staleClaim = new Claim(SecurityRoleSeeder.PermissionClaimType, Permissions.EditSponsor.Value);
        var existingClaims = new[] { Permissions.CreateArticle, Permissions.EditArticle, Permissions.DeleteArticle }
            .Select(p => new Claim(SecurityRoleSeeder.PermissionClaimType, p.Value))
            .Append(staleClaim)
            .ToList();

        var roleManagerMock = CreateRoleManagerMock();
        SetupOtherRolesAlreadySynced(roleManagerMock, Roles.Journalist);

        roleManagerMock
            .Setup(m => m.FindByNameAsync(Roles.Journalist))
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
    }
}