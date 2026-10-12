using System.Security.Claims;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Contracts.Security;
using Neba.Api.Security.Domain;
using Neba.Api.Security.GetUser;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Security.GetUser;

[IntegrationTest]
[Component("Security")]
[Collection<SecurityDbContextFixture>]
public sealed class GetUserQueryHandlerTests(SecurityDbContextFixture fixture)
    : IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    [Fact(DisplayName = "HandleAsync returns UserNotFound when no user has the ID")]
    public async Task HandleAsync_ShouldReturnUserNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = new GetUserQueryHandler(fixture.CreateDbContext());

        // Act
        var result = await handler.HandleAsync(new GetUserQuery { UserId = Ulid.NewUlid() }, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(GetUserErrors.UserNotFound);
    }

    [Fact(DisplayName = "HandleAsync returns the user's roles and added and removed permissions")]
    public async Task HandleAsync_ShouldReturnRolesAndOverrides_WhenUserHasThem()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        await roleManager.CreateAsync(new ApplicationRole(Roles.Journalist));
        var user = ApplicationUserFactory.Create(emailConfirmed: true);
        await userManager.CreateAsync(user);
        await userManager.AddToRoleAsync(user, Roles.Journalist);
        await userManager.AddClaimsAsync(user,
        [
            new Claim(Permissions.ClaimType, Permissions.CreateSponsor.Value),
            new Claim(Permissions.DeniedClaimType, Permissions.CreateArticle.Value)
        ]);

        var handler = new GetUserQueryHandler(fixture.CreateDbContext());

        // Act
        var result = await handler.HandleAsync(new GetUserQuery { UserId = user.Id }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.UserId.ShouldBe(user.Id);
        result.Value.Email.ShouldBe(user.Email);
        result.Value.EmailConfirmed.ShouldBeTrue();
        result.Value.Roles.ShouldBe([Roles.Journalist]);
        result.Value.AddedPermissions.ShouldBe([Permissions.CreateSponsor.Value]);
        result.Value.RemovedPermissions.ShouldBe([Permissions.CreateArticle.Value]);
    }

    [Fact(DisplayName = "HandleAsync omits a stored override whose value is no longer a known permission")]
    public async Task HandleAsync_ShouldOmitUnknownStoredValue()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = ApplicationUserFactory.Create();
        await userManager.CreateAsync(user);
        await userManager.AddClaimAsync(user, new Claim(Permissions.ClaimType, "Retired.Permission"));

        var handler = new GetUserQueryHandler(fixture.CreateDbContext());

        // Act
        var result = await handler.HandleAsync(new GetUserQuery { UserId = user.Id }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.AddedPermissions.ShouldBeEmpty();
        result.Value.RemovedPermissions.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync ignores claims of other types and other users")]
    public async Task HandleAsync_ShouldIgnoreOtherClaimTypesAndUsers()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = ApplicationUserFactory.Create();
        var other = ApplicationUserFactory.Create(userName: "other@bowlneba.com", email: "other@bowlneba.com");
        await userManager.CreateAsync(user);
        await userManager.CreateAsync(other);
        await userManager.AddClaimAsync(user, new Claim("something-else", Permissions.CreateArticle.Value));
        await userManager.AddClaimAsync(other, new Claim(Permissions.ClaimType, Permissions.CreateArticle.Value));

        var handler = new GetUserQueryHandler(fixture.CreateDbContext());

        // Act
        var result = await handler.HandleAsync(new GetUserQuery { UserId = user.Id }, ct);

        // Assert
        result.Value.AddedPermissions.ShouldBeEmpty();
        result.Value.RemovedPermissions.ShouldBeEmpty();
        result.Value.Roles.ShouldBeEmpty();
    }
}
