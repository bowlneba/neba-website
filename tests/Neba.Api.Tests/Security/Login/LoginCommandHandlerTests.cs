using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Security;
using Neba.Api.Security.Domain;
using Neba.Api.Security.Login;
using Neba.Api.Security.Register;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Security.Login;

[IntegrationTest]
[Component("Security")]
[Collection<SecurityDbContextFixture>]
public sealed class LoginCommandHandlerIntegrationTests(SecurityDbContextFixture fixture)
    : IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
    private const string RefreshTokenProvider = "RefreshToken";
    private const string RefreshTokenName = "RefreshToken";

    private static readonly JwtSettings TestJwtSettings = new()
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        SigningKey = "test-signing-key-must-be-at-least-32-bytes-long!",
        AccessTokenExpiryMinutes = 15,
        RefreshTokenExpiryDays = 7
    };

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    private static LoginCommandHandler CreateHandler(UserManager<ApplicationUser> userManager)
        => new(userManager, new JwtTokenService(TestJwtSettings, TimeProvider.System), TimeProvider.System);

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

    [Fact(DisplayName = "HandleAsync returns LoginDto with non-empty tokens when credentials are valid")]
    public async Task HandleAsync_ShouldReturnLoginDto_WhenCredentialsAreValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await SeedUserAsync(userManager);
        var command = new LoginCommand
        {
            Email = RegisterRequestFactory.ValidEmail,
            Password = RegisterRequestFactory.ValidPassword
        };

        // Act
        var result = await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.AccessToken.ShouldNotBeNullOrEmpty();
        result.Value.RefreshToken.ShouldNotBeNullOrEmpty();
        result.Value.ExpiresAt.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
    }

    [Fact(DisplayName = "HandleAsync returns LoginDto with email and userId matching the registered user")]
    public async Task HandleAsync_ShouldReturnLoginDto_WithCorrectEmailAndUserId()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        var command = new LoginCommand
        {
            Email = RegisterRequestFactory.ValidEmail,
            Password = RegisterRequestFactory.ValidPassword
        };

        // Act
        var result = await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Email.ShouldBe(RegisterRequestFactory.ValidEmail);
        result.Value.UserId.ShouldBe(user.Id);
    }

    [Fact(DisplayName = "HandleAsync stores a hashed refresh token via UserManager after successful login")]
    public async Task HandleAsync_ShouldStoreHashedRefreshToken_AfterSuccessfulLogin()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await SeedUserAsync(userManager);
        var command = new LoginCommand
        {
            Email = RegisterRequestFactory.ValidEmail,
            Password = RegisterRequestFactory.ValidPassword
        };

        // Act
        var result = await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();

        var user = await userManager.FindByEmailAsync(command.Email);
        var storedJson = await userManager.GetAuthenticationTokenAsync(user!, RefreshTokenProvider, RefreshTokenName);
        storedJson.ShouldNotBeNullOrEmpty();

        var stored = JsonSerializer.Deserialize<StoredRefreshToken>(storedJson!);
        stored.ShouldNotBeNull();

        var expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(result.Value.RefreshToken)));
        stored!.Hash.ShouldBe(expectedHash);
    }

    [Fact(DisplayName = "HandleAsync returns InvalidCredentials when the email is not registered")]
    public async Task HandleAsync_ShouldReturnInvalidCredentials_WhenEmailNotFound()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var command = new LoginCommand
        {
            Email = "nobody@bowlneba.com",
            Password = RegisterRequestFactory.ValidPassword
        };

        // Act
        var result = await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorOr.ErrorType.Unauthorized);
        result.FirstError.Code.ShouldBe("Login.InvalidCredentials");
    }

    [Fact(DisplayName = "HandleAsync returns InvalidCredentials when the password is wrong")]
    public async Task HandleAsync_ShouldReturnInvalidCredentials_WhenPasswordIsIncorrect()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await SeedUserAsync(userManager);
        var command = new LoginCommand
        {
            Email = RegisterRequestFactory.ValidEmail,
            Password = "WrongPassword9"
        };

        // Act
        var result = await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorOr.ErrorType.Unauthorized);
        result.FirstError.Code.ShouldBe("Login.InvalidCredentials");
    }
}