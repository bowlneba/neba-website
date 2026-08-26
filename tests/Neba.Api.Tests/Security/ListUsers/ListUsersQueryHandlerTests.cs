using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Security.Domain;
using Neba.Api.Security.ListUsers;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;

namespace Neba.Api.Tests.Security.ListUsers;

[IntegrationTest]
[Component("Security")]
[Collection<SecurityDbContextFixture>]
public sealed class ListUsersQueryHandlerTests(SecurityDbContextFixture fixture)
    : IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    [Fact(DisplayName = "HandleAsync returns empty collection when no users exist")]
    public async Task HandleAsync_ShouldReturnEmpty_WhenNoUsersExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = new ListUsersQueryHandler(fixture.CreateDbContext());

        // Act
        var result = await handler.HandleAsync(new ListUsersQuery(), ct);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync returns each user's email, confirmation status, and roles")]
    public async Task HandleAsync_ShouldReturnEmailConfirmationAndRoles_WhenUsersExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        await roleManager.CreateAsync(new ApplicationRole(Roles.Webmaster));

        var user = new ApplicationUser
        {
            Id = Ulid.NewUlid(),
            UserName = "webmaster@bowlneba.com",
            Email = "webmaster@bowlneba.com",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(user);
        await userManager.AddToRoleAsync(user, Roles.Webmaster);

        var handler = new ListUsersQueryHandler(fixture.CreateDbContext());

        // Act
        var result = await handler.HandleAsync(new ListUsersQuery(), ct);

        // Assert
        result.ShouldHaveSingleItem();
        var dto = result.Single();
        dto.UserId.ShouldBe(user.Id);
        dto.Email.ShouldBe("webmaster@bowlneba.com");
        dto.EmailConfirmed.ShouldBeTrue();
        dto.Roles.ShouldBe([Roles.Webmaster]);
    }

    [Fact(DisplayName = "HandleAsync returns users ordered by email")]
    public async Task HandleAsync_ShouldReturnUsersOrderedByEmail()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await userManager.CreateAsync(new ApplicationUser
        {
            Id = Ulid.NewUlid(),
            UserName = "zed@bowlneba.com",
            Email = "zed@bowlneba.com"
        });
        await userManager.CreateAsync(new ApplicationUser
        {
            Id = Ulid.NewUlid(),
            UserName = "amy@bowlneba.com",
            Email = "amy@bowlneba.com"
        });

        var handler = new ListUsersQueryHandler(fixture.CreateDbContext());

        // Act
        var result = await handler.HandleAsync(new ListUsersQuery(), ct);

        // Assert
        result.Select(u => u.Email).ShouldBe(["amy@bowlneba.com", "zed@bowlneba.com"]);
    }

    [Fact(DisplayName = "HandleAsync returns an empty roles collection for a user with no role assignments")]
    public async Task HandleAsync_ShouldReturnEmptyRoles_WhenUserHasNoRoleAssignments()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await userManager.CreateAsync(new ApplicationUser
        {
            Id = Ulid.NewUlid(),
            UserName = "unassigned@bowlneba.com",
            Email = "unassigned@bowlneba.com"
        });

        var handler = new ListUsersQueryHandler(fixture.CreateDbContext());

        // Act
        var result = await handler.HandleAsync(new ListUsersQuery(), ct);

        // Assert
        result.ShouldHaveSingleItem().Roles.ShouldBeEmpty();
    }
}