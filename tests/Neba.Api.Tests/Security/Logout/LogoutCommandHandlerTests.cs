using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Security;
using Neba.Api.Security.Domain;
using Neba.Api.Security.Login;
using Neba.Api.Security.Logout;
using Neba.Api.Security.Register;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Security.Logout;

[IntegrationTest]
[Component("Security")]
[Collection<SecurityDbContextFixture>]
public sealed class LogoutCommandHandlerIntegrationTests(SecurityDbContextFixture fixture)
    : IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
    private const string RefreshTokenProvider = "RefreshToken";
    private const string RefreshTokenName = "RefreshToken";

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    private static LogoutCommandHandler CreateHandler(UserManager<ApplicationUser> userManager)
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
        user!.EmailConfirmed = true;
        await userManager.UpdateAsync(user);
        return user;
    }

    private static async Task<(ApplicationUser User, string RefreshToken)> SeedLoginAsync(
        UserManager<ApplicationUser> userManager)
    {
        var user = await SeedUserAsync(userManager);
        var loginResult = await new LoginCommandHandler(
                userManager,
                new JwtTokenService(
                    new JwtSettings
                    {
                        Issuer = "test-issuer",
                        Audience = "test-audience",
                        SigningKey = "test-signing-key-must-be-at-least-32-bytes-long!",
                        AccessTokenExpiryMinutes = 15,
                        RefreshTokenExpiryDays = 7
                    },
                    TimeProvider.System),
                TimeProvider.System)
            .HandleAsync(
                new LoginCommand
                {
                    Email = RegisterRequestFactory.ValidEmail,
                    Password = RegisterRequestFactory.ValidPassword
                },
                CancellationToken.None);
        return (user, loginResult.Value.RefreshToken);
    }

    [Fact(DisplayName = "HandleAsync returns Success and removes the stored refresh token when the user exists")]
    public async Task HandleAsync_ShouldReturnSuccessAndRemoveToken_WhenUserExists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var (user, _) = await SeedLoginAsync(userManager);
        var command = new LogoutCommand { UserId = user.Id };

        // Act
        var result = await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var storedToken = await userManager.GetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName);
        storedToken.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync returns Success when the user does not exist")]
    public async Task HandleAsync_ShouldReturnSuccess_WhenUserDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var command = new LogoutCommand { UserId = Ulid.NewUlid() };

        // Act
        var result = await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync returns Success when the user has no stored refresh token")]
    public async Task HandleAsync_ShouldReturnSuccess_WhenUserHasNoStoredToken()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        var command = new LogoutCommand { UserId = user.Id };

        // Act
        var result = await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var storedToken = await userManager.GetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName);
        storedToken.ShouldBeNull();
    }
}
