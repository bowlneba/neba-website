using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Security.Domain;
using Neba.Api.Security.GetCurrentUser;
using Neba.Api.Security.Register;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Security.GetCurrentUser;

[IntegrationTest]
[Component("Security")]
[Collection<SecurityDbContextFixture>]
public sealed class GetCurrentUserQueryHandlerIntegrationTests(SecurityDbContextFixture fixture)
    : IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    private static GetCurrentUserQueryHandler CreateHandler(UserManager<ApplicationUser> userManager)
        => new(userManager);

    private static async Task<ApplicationUser> SeedUserAsync(UserManager<ApplicationUser> userManager)
    {
        var command = new RegisterCommand
        {
            Email = RegisterRequestFactory.ValidEmail,
            Password = RegisterRequestFactory.ValidPassword
        };
        await new RegisterCommandHandler(userManager).HandleAsync(command, CancellationToken.None);

        var user = await userManager.FindByEmailAsync(command.Email);
        return user!;
    }

    [Fact(DisplayName = "HandleAsync returns UserNotFound when user does not exist")]
    public async Task HandleAsync_ShouldReturnUserNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var query = new GetCurrentUserQuery { UserId = Ulid.NewUlid() };

        // Act
        var result = await CreateHandler(userManager).HandleAsync(query, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorOr.ErrorType.NotFound);
        result.FirstError.Code.ShouldBe("Security.UserNotFound");
    }

    [Fact(DisplayName = "HandleAsync returns UserDto with correct UserId and Email when user exists")]
    public async Task HandleAsync_ShouldReturnUserDto_WithCorrectUserIdAndEmail_WhenUserExists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        var query = new GetCurrentUserQuery { UserId = user.Id };

        // Act
        var result = await CreateHandler(userManager).HandleAsync(query, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.UserId.ShouldBe(user.Id);
        result.Value.Email.ShouldBe(RegisterRequestFactory.ValidEmail);
    }

    [Fact(DisplayName = "HandleAsync returns UserDto with empty Roles when user has no roles")]
    public async Task HandleAsync_ShouldReturnUserDtoWithEmptyRoles_WhenUserHasNoRoles()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        var query = new GetCurrentUserQuery { UserId = user.Id };

        // Act
        var result = await CreateHandler(userManager).HandleAsync(query, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Roles.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync returns UserDto with assigned roles when user has roles")]
    public async Task HandleAsync_ShouldReturnUserDtoWithRoles_WhenUserHasRoles()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var user = await SeedUserAsync(userManager);

        await roleManager.CreateAsync(ApplicationRoleFactory.Create(name: Roles.Admin));
        await roleManager.CreateAsync(ApplicationRoleFactory.Create(name: Roles.Member));
        await userManager.AddToRoleAsync(user, Roles.Admin);
        await userManager.AddToRoleAsync(user, Roles.Member);

        var query = new GetCurrentUserQuery { UserId = user.Id };

        // Act
        var result = await CreateHandler(userManager).HandleAsync(query, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Roles.ShouldContain(Roles.Admin);
        result.Value.Roles.ShouldContain(Roles.Member);
        result.Value.Roles.Count.ShouldBe(2);
    }

    [Fact(DisplayName = "HandleAsync returns UserDto with null UsbcId when user has no UsbcId")]
    public async Task HandleAsync_ShouldReturnUserDtoWithNullUsbcId_WhenUserHasNoUsbcId()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        var query = new GetCurrentUserQuery { UserId = user.Id };

        // Act
        var result = await CreateHandler(userManager).HandleAsync(query, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.UsbcId.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync returns UserDto with UsbcId when user has a UsbcId")]
    public async Task HandleAsync_ShouldReturnUserDtoWithUsbcId_WhenUserHasUsbcId()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = ApplicationUserFactory.Create(usbcId: "123-45678");
        await userManager.CreateAsync(user, RegisterRequestFactory.ValidPassword);
        var query = new GetCurrentUserQuery { UserId = user.Id };

        // Act
        var result = await CreateHandler(userManager).HandleAsync(query, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.UsbcId.ShouldBe("123-45678");
    }
}