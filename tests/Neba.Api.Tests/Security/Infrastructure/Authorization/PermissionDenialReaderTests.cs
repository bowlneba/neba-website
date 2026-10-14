using System.Security.Claims;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Contracts.Security;
using Neba.Api.Security.Domain;
using Neba.Api.Security.Infrastructure.Authorization;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Security.Infrastructure.Authorization;

[IntegrationTest]
[Component("Security")]
[Collection<SecurityDbContextFixture>]
public sealed class PermissionDenialReaderTests(SecurityDbContextFixture fixture)
    : IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

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

    private async Task<bool> IsDeniedAsync(Ulid userId, Permissions permission)
    {
        await using var context = fixture.CreateDbContext();
        var reader = new PermissionDenialReader(context);

        return await reader.IsDeniedAsync(userId, permission, TestContext.Current.CancellationToken);
    }

    [Fact(DisplayName = "IsDeniedAsync returns true when the user has a stored denial for the permission")]
    public async Task IsDeniedAsync_ShouldReturnTrue_WhenDenialStored()
    {
        // Arrange
        var user = await SeedUserAsync(new Claim(Permissions.DeniedClaimType, Permissions.EditArticle.Value));

        // Act
        var result = await IsDeniedAsync(user.Id, Permissions.EditArticle);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "IsDeniedAsync returns false when the user has no denial")]
    public async Task IsDeniedAsync_ShouldReturnFalse_WhenNoDenialStored()
    {
        // Arrange
        var user = await SeedUserAsync();

        // Act
        var result = await IsDeniedAsync(user.Id, Permissions.EditArticle);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "IsDeniedAsync returns false when the stored denial is for a different permission")]
    public async Task IsDeniedAsync_ShouldReturnFalse_WhenDenialIsForDifferentPermission()
    {
        // Arrange
        var user = await SeedUserAsync(new Claim(Permissions.DeniedClaimType, Permissions.CreateArticle.Value));

        // Act
        var result = await IsDeniedAsync(user.Id, Permissions.EditArticle);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "IsDeniedAsync returns false when the stored claim is a grant, not a denial")]
    public async Task IsDeniedAsync_ShouldReturnFalse_WhenClaimIsGrant()
    {
        // Arrange
        var user = await SeedUserAsync(new Claim(Permissions.ClaimType, Permissions.EditArticle.Value));

        // Act
        var result = await IsDeniedAsync(user.Id, Permissions.EditArticle);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "IsDeniedAsync returns false when another user holds the denial")]
    public async Task IsDeniedAsync_ShouldReturnFalse_WhenAnotherUserHoldsDenial()
    {
        // Arrange
        await SeedUserAsync(new Claim(Permissions.DeniedClaimType, Permissions.EditArticle.Value));
        var caller = await SeedUserAsync();

        // Act
        var result = await IsDeniedAsync(caller.Id, Permissions.EditArticle);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "IsDeniedAsync returns false for a stored Admin even when a denial is stored")]
    public async Task IsDeniedAsync_ShouldReturnFalse_WhenUserHoldsStoredAdminRole()
    {
        // Arrange
        var user = await SeedUserAsync(new Claim(Permissions.DeniedClaimType, Permissions.EditArticle.Value));

        using (var scope = fixture.CreateScope())
        {
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            await roleManager.CreateAsync(ApplicationRoleFactory.Create(name: Roles.Admin));

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var storedUser = await userManager.FindByIdAsync(user.Id.ToString());
            await userManager.AddToRoleAsync(storedUser!, Roles.Admin);
        }

        // Act
        var result = await IsDeniedAsync(user.Id, Permissions.EditArticle);

        // Assert
        result.ShouldBeFalse();
    }
}
