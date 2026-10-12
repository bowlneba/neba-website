using System.Security.Claims;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Contracts.Security;
using Neba.Api.Security.Domain;
using Neba.Api.Security.ListRoles;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;

namespace Neba.Api.Tests.Security.ListRoles;

[IntegrationTest]
[Component("Security")]
[Collection<SecurityDbContextFixture>]
public sealed class ListRolesQueryHandlerTests(SecurityDbContextFixture fixture)
    : IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    [Fact(DisplayName = "HandleAsync returns an empty list when no roles exist")]
    public async Task HandleAsync_ShouldReturnEmptyList_WhenNoRolesExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = new ListRolesQueryHandler(fixture.CreateDbContext());

        // Act
        var result = await handler.HandleAsync(new ListRolesQuery(), ct);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync groups permission claims per role, ordered by role name and value")]
    public async Task HandleAsync_ShouldGroupPermissionClaimsPerRole()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        var journalist = new ApplicationRole(Roles.Journalist);
        var manager = new ApplicationRole(Roles.Manager);
        await roleManager.CreateAsync(manager);
        await roleManager.CreateAsync(journalist);
        await roleManager.AddClaimAsync(journalist, new Claim(Permissions.ClaimType, Permissions.EditArticle.Value));
        await roleManager.AddClaimAsync(journalist, new Claim(Permissions.ClaimType, Permissions.CreateArticle.Value));
        await roleManager.AddClaimAsync(manager, new Claim(Permissions.ClaimType, Permissions.CreateSponsor.Value));

        var handler = new ListRolesQueryHandler(fixture.CreateDbContext());

        // Act
        var result = await handler.HandleAsync(new ListRolesQuery(), ct);

        // Assert
        result.Select(r => r.Name).ShouldBe([Roles.Journalist, Roles.Manager]);
        result.First().Permissions.ShouldBe([Permissions.CreateArticle.Value, Permissions.EditArticle.Value]);
        result.Last().Permissions.ShouldBe([Permissions.CreateSponsor.Value]);
    }

    [Fact(DisplayName = "HandleAsync ignores role claims that are not permissions and returns an empty list for a role with none")]
    public async Task HandleAsync_ShouldIgnoreNonPermissionClaims()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        var member = new ApplicationRole(Roles.Member);
        var journalist = new ApplicationRole(Roles.Journalist);
        await roleManager.CreateAsync(member);
        await roleManager.CreateAsync(journalist);
        await roleManager.AddClaimAsync(journalist, new Claim("something-else", Permissions.CreateArticle.Value));

        var handler = new ListRolesQueryHandler(fixture.CreateDbContext());

        // Act
        var result = await handler.HandleAsync(new ListRolesQuery(), ct);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldAllBe(role => role.Permissions.Count == 0);
    }
}
