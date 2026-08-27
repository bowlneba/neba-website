using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Security.Domain;
using Neba.Api.Security.ListUsers;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Security.ListUsers;

[IntegrationTest]
[Component("Security")]
[Collection<SecurityDbContextFixture>]
public sealed class ListUsersQueryHandlerTests(SecurityDbContextFixture fixture)
    : IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
    private const int DefaultPageSize = 20;

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    [Fact(DisplayName = "HandleAsync returns empty page when no users exist")]
    public async Task HandleAsync_ShouldReturnEmptyPage_WhenNoUsersExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = new ListUsersQueryHandler(fixture.CreateDbContext());

        // Act
        var result = await handler.HandleAsync(new ListUsersQuery { Page = 1, PageSize = DefaultPageSize }, ct);

        // Assert
        result.Items.ShouldBeEmpty();
        result.TotalItems.ShouldBe(0);
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

        var user = ApplicationUserFactory.Create(userName: "webmaster@bowlneba.com", email: "webmaster@bowlneba.com", emailConfirmed: true);
        await userManager.CreateAsync(user);
        await userManager.AddToRoleAsync(user, Roles.Webmaster);

        var handler = new ListUsersQueryHandler(fixture.CreateDbContext());

        // Act
        var result = await handler.HandleAsync(new ListUsersQuery { Page = 1, PageSize = DefaultPageSize }, ct);

        // Assert
        result.TotalItems.ShouldBe(1);
        var dto = result.Items.ShouldHaveSingleItem();
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

        await userManager.CreateAsync(ApplicationUserFactory.Create(userName: "zed@bowlneba.com", email: "zed@bowlneba.com"));
        await userManager.CreateAsync(ApplicationUserFactory.Create(userName: "amy@bowlneba.com", email: "amy@bowlneba.com"));

        var handler = new ListUsersQueryHandler(fixture.CreateDbContext());

        // Act
        var result = await handler.HandleAsync(new ListUsersQuery { Page = 1, PageSize = DefaultPageSize }, ct);

        // Assert
        result.Items.Select(u => u.Email).ShouldBe(["amy@bowlneba.com", "zed@bowlneba.com"]);
    }

    [Fact(DisplayName = "HandleAsync returns an empty roles collection for a user with no role assignments")]
    public async Task HandleAsync_ShouldReturnEmptyRoles_WhenUserHasNoRoleAssignments()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await userManager.CreateAsync(ApplicationUserFactory.Create(userName: "unassigned@bowlneba.com", email: "unassigned@bowlneba.com"));

        var handler = new ListUsersQueryHandler(fixture.CreateDbContext());

        // Act
        var result = await handler.HandleAsync(new ListUsersQuery { Page = 1, PageSize = DefaultPageSize }, ct);

        // Assert
        result.Items.ShouldHaveSingleItem().Roles.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync returns only the requested page of users, with the correct total")]
    public async Task HandleAsync_ShouldReturnOnlyRequestedPage_WithCorrectTotalItems()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var email in new[] { "a@bowlneba.com", "b@bowlneba.com", "c@bowlneba.com" })
        {
            await userManager.CreateAsync(ApplicationUserFactory.Create(userName: email, email: email));
        }

        var handler = new ListUsersQueryHandler(fixture.CreateDbContext());

        // Act
        var result = await handler.HandleAsync(new ListUsersQuery { Page = 2, PageSize = 2 }, ct);

        // Assert
        result.TotalItems.ShouldBe(3);
        var dto = result.Items.ShouldHaveSingleItem();
        dto.Email.ShouldBe("c@bowlneba.com");
    }
}