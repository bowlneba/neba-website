using System.Security.Claims;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Contracts.Security;
using Neba.Api.Security.Domain;
using Neba.Api.Security.Infrastructure;
using Neba.Api.Security.Infrastructure.Authorization;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Security.Infrastructure.Authorization;

[IntegrationTest]
[Component("Security")]
[Collection<SecurityDbContextFixture>]
public sealed class PermissionResolverTests(SecurityDbContextFixture fixture)
    : IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    private async Task SeedRoleAsync(string roleName, params string[] permissionValues)
    {
        using var scope = fixture.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        var role = ApplicationRoleFactory.Create(name: roleName);
        await roleManager.CreateAsync(role);

        foreach (var value in permissionValues)
        {
            await roleManager.AddClaimAsync(role, new Claim(SecurityRoleSeeder.PermissionClaimType, value));
        }
    }

    private async Task<ApplicationUser> SeedUserAsync(params Claim[] claims)
    {
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = ApplicationUserFactory.Create();
        await userManager.CreateAsync(user);

        if (claims.Length > 0)
        {
            await userManager.AddClaimsAsync(user, claims);
        }

        return user;
    }

    private async Task<IReadOnlyCollection<Permissions>> ResolveAsync(ApplicationUser user, params string[] roleNames)
    {
        await using var context = fixture.CreateDbContext();
        return await PermissionResolver.ResolveAsync(context, user, roleNames, TestContext.Current.CancellationToken);
    }

    [Fact(DisplayName = "ResolveAsync should return an empty collection when no role names are provided")]
    public async Task ResolveAsync_ShouldReturnEmptyCollection_WhenNoRoleNamesProvided()
    {
        // Arrange
        var user = await SeedUserAsync();

        // Act
        var result = await ResolveAsync(user);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "ResolveAsync should skip a role name that does not resolve to a role")]
    public async Task ResolveAsync_ShouldSkipRole_WhenRoleNotFound()
    {
        // Arrange
        var user = await SeedUserAsync();

        // Act
        var result = await ResolveAsync(user, Roles.Admin);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "ResolveAsync should return the permission claim values for a resolved role")]
    public async Task ResolveAsync_ShouldReturnPermissionClaimValues_WhenRoleResolves()
    {
        // Arrange
        await SeedRoleAsync(Roles.Admin, Permissions.CreateArticle.Value);
        var user = await SeedUserAsync();

        // Act
        var result = await ResolveAsync(user, Roles.Admin);

        // Assert
        result.ShouldBe([Permissions.CreateArticle]);
    }

    [Fact(DisplayName = "ResolveAsync should match role names regardless of case")]
    public async Task ResolveAsync_ShouldMatchRoleNames_RegardlessOfCase()
    {
        // Arrange
        await SeedRoleAsync(Roles.Manager, Permissions.CreateArticle.Value);
        var user = await SeedUserAsync();

        // Act
        var result = await ResolveAsync(user, Roles.Manager.ToUpperInvariant());

        // Assert
        result.ShouldBe([Permissions.CreateArticle]);
    }

    [Fact(DisplayName = "ResolveAsync should ignore role claims whose claim type is not the permission claim type")]
    public async Task ResolveAsync_ShouldIgnoreNonPermissionClaims()
    {
        // Arrange
        using (var scope = fixture.CreateScope())
        {
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var role = ApplicationRoleFactory.Create(name: Roles.Admin);
            await roleManager.CreateAsync(role);
            await roleManager.AddClaimAsync(role, new Claim(ClaimTypes.Role, Roles.Admin));
        }

        var user = await SeedUserAsync();

        // Act
        var result = await ResolveAsync(user, Roles.Admin);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "ResolveAsync should not return permissions from roles the user does not hold")]
    public async Task ResolveAsync_ShouldNotReturnPermissions_FromRolesNotRequested()
    {
        // Arrange
        await SeedRoleAsync(Roles.Journalist, Permissions.CreateArticle.Value);
        await SeedRoleAsync(Roles.Manager, Permissions.CreateSponsor.Value);
        var user = await SeedUserAsync();

        // Act
        var result = await ResolveAsync(user, Roles.Journalist);

        // Assert
        result.ShouldBe([Permissions.CreateArticle]);
    }

    [Fact(DisplayName = "ResolveAsync should deduplicate permission keys shared across multiple roles")]
    public async Task ResolveAsync_ShouldDeduplicatePermissionKeys_AcrossMultipleRoles()
    {
        // Arrange
        await SeedRoleAsync(Roles.Admin, Permissions.CreateArticle.Value);
        await SeedRoleAsync(Roles.Member, Permissions.CreateArticle.Value);
        var user = await SeedUserAsync();

        // Act
        var result = await ResolveAsync(user, Roles.Admin, Roles.Member);

        // Assert
        result.ShouldBe([Permissions.CreateArticle]);
    }

    [Fact(DisplayName = "ResolveAsync should aggregate distinct permission keys across multiple roles")]
    public async Task ResolveAsync_ShouldAggregateDistinctPermissionKeys_AcrossMultipleRoles()
    {
        // Arrange
        await SeedRoleAsync(Roles.Admin, Permissions.CreateArticle.Value);
        await SeedRoleAsync(Roles.Member, Permissions.EditArticle.Value);
        var user = await SeedUserAsync();

        // Act
        var result = await ResolveAsync(user, Roles.Admin, Roles.Member);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(Permissions.CreateArticle);
        result.ShouldContain(Permissions.EditArticle);
    }

    [Fact(DisplayName = "ResolveAsync should add a permission the user has been granted outside their roles")]
    public async Task ResolveAsync_ShouldAddUserAddedPermission()
    {
        // Arrange
        await SeedRoleAsync(Roles.Journalist, Permissions.CreateArticle.Value);
        var user = await SeedUserAsync(new Claim(Permissions.ClaimType, Permissions.CreateSponsor.Value));

        // Act
        var result = await ResolveAsync(user, Roles.Journalist);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(Permissions.CreateArticle);
        result.ShouldContain(Permissions.CreateSponsor);
    }

    [Fact(DisplayName = "ResolveAsync should remove a role permission the user has been denied")]
    public async Task ResolveAsync_ShouldRemoveUserDeniedPermission()
    {
        // Arrange
        await SeedRoleAsync(Roles.Journalist, Permissions.CreateArticle.Value, Permissions.EditArticle.Value);
        var user = await SeedUserAsync(new Claim(Permissions.DeniedClaimType, Permissions.CreateArticle.Value));

        // Act
        var result = await ResolveAsync(user, Roles.Journalist);

        // Assert
        result.ShouldBe([Permissions.EditArticle]);
    }

    [Fact(DisplayName = "ResolveAsync should ignore a stored denial for a user who holds the Admin role")]
    public async Task ResolveAsync_ShouldIgnoreStoredDenial_WhenUserIsAdmin()
    {
        // Arrange
        await SeedRoleAsync(Roles.Admin, Permissions.CreateArticle.Value);
        var user = await SeedUserAsync(new Claim(Permissions.DeniedClaimType, Permissions.CreateArticle.Value));

        // Act
        var result = await ResolveAsync(user, Roles.Admin);

        // Assert
        result.ShouldBe([Permissions.CreateArticle]);
    }

    [Fact(DisplayName = "ResolveAsync should skip a stored override whose value is no longer a known permission")]
    public async Task ResolveAsync_ShouldSkipUnknownStoredValue()
    {
        // Arrange
        await SeedRoleAsync(Roles.Journalist, Permissions.CreateArticle.Value);
        var user = await SeedUserAsync(new Claim(Permissions.ClaimType, "Retired.Permission"));

        // Act
        var result = await ResolveAsync(user, Roles.Journalist);

        // Assert
        result.ShouldBe([Permissions.CreateArticle]);
    }

    [Fact(DisplayName = "ResolveAsync should let a failed claim read propagate instead of falling back to role-only permissions")]
    public async Task ResolveAsync_ShouldPropagate_WhenClaimReadFails()
    {
        // Arrange
        await SeedRoleAsync(Roles.Journalist, Permissions.CreateArticle.Value);
        var user = await SeedUserAsync();
        var context = fixture.CreateDbContext();
        await context.DisposeAsync();

        // Act
        var act = () => PermissionResolver.ResolveAsync(
            context, user, [Roles.Journalist], TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<ObjectDisposedException>(act);
    }
}