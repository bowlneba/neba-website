using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Contracts.Security;
using Neba.Api.Security;
using Neba.Api.Security.Domain;
using Neba.Api.Security.Login;
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

    private LoginCommandHandler CreateHandler(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
        => new(userManager, fixture.CreateDbContext(), signInManager, new JwtTokenService(TestJwtSettings, TimeProvider.System), TimeProvider.System);

    private static async Task<ApplicationUser> SeedUserAsync(UserManager<ApplicationUser> userManager)
    {
        var user = ApplicationUserFactory.Create(userName: LoginRequestFactory.ValidEmail, email: LoginRequestFactory.ValidEmail);
        user.EmailConfirmed = true;
        await userManager.CreateAsync(user, LoginRequestFactory.ValidPassword);
        return user;
    }

    [Fact(DisplayName = "HandleAsync returns LoginDto with non-empty tokens when credentials are valid")]
    public async Task HandleAsync_ShouldReturnLoginDto_WhenCredentialsAreValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        await SeedUserAsync(userManager);
        var command = new LoginCommand
        {
            Email = LoginRequestFactory.ValidEmail,
            Password = LoginRequestFactory.ValidPassword
        };

        // Act
        var result = await CreateHandler(userManager, signInManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.AccessToken.ShouldNotBeNullOrEmpty();
        result.Value.RefreshToken.ShouldNotBeNullOrEmpty();
        result.Value.ExpiresAt.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
    }

    [Fact(DisplayName = "HandleAsync issues a token with role permissions plus added minus removed overrides")]
    public async Task HandleAsync_ShouldApplyPermissionOverrides_WhenUserHasOverrides()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        var role = new ApplicationRole(Roles.Journalist);
        await roleManager.CreateAsync(role);
        await roleManager.AddClaimAsync(role, new Claim(Permissions.ClaimType, Permissions.CreateArticle.Value));
        await roleManager.AddClaimAsync(role, new Claim(Permissions.ClaimType, Permissions.EditArticle.Value));

        var user = await SeedUserAsync(userManager);
        await userManager.AddToRoleAsync(user, Roles.Journalist);
        await userManager.AddClaimsAsync(user,
        [
            new Claim(Permissions.ClaimType, Permissions.CreateSponsor.Value),
            new Claim(Permissions.DeniedClaimType, Permissions.CreateArticle.Value)
        ]);

        var command = new LoginCommand
        {
            Email = LoginRequestFactory.ValidEmail,
            Password = LoginRequestFactory.ValidPassword
        };

        // Act
        var result = await CreateHandler(userManager, signInManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Value.AccessToken);
        var permissions = token.Claims.Where(c => c.Type == Permissions.ClaimType).Select(c => c.Value).ToList();
        permissions.Count.ShouldBe(2);
        permissions.ShouldContain(Permissions.EditArticle.Value);
        permissions.ShouldContain(Permissions.CreateSponsor.Value);
        permissions.ShouldNotContain(Permissions.CreateArticle.Value);
    }

    [Fact(DisplayName = "HandleAsync returns LoginDto with email and userId matching the registered user")]
    public async Task HandleAsync_ShouldReturnLoginDto_WithCorrectEmailAndUserId()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        var command = new LoginCommand
        {
            Email = LoginRequestFactory.ValidEmail,
            Password = LoginRequestFactory.ValidPassword
        };

        // Act
        var result = await CreateHandler(userManager, signInManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Email.ShouldBe(LoginRequestFactory.ValidEmail);
        result.Value.UserId.ShouldBe(user.Id);
    }

    [Fact(DisplayName = "HandleAsync stores a hashed refresh token via UserManager after successful login")]
    public async Task HandleAsync_ShouldStoreHashedRefreshToken_AfterSuccessfulLogin()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        await SeedUserAsync(userManager);
        var command = new LoginCommand
        {
            Email = LoginRequestFactory.ValidEmail,
            Password = LoginRequestFactory.ValidPassword
        };

        // Act
        var result = await CreateHandler(userManager, signInManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();

        var user = await userManager.FindByEmailAsync(command.Email);
        var storedJson = await userManager.GetAuthenticationTokenAsync(user!, RefreshTokenProvider, RefreshTokenName);
        storedJson.ShouldNotBeNullOrEmpty();

        var stored = JsonSerializer.Deserialize<StoredRefreshToken>(storedJson);
        stored.ShouldNotBeNull();

        var expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(result.Value.RefreshToken)));
        var slot = stored.Slots.ShouldHaveSingleItem();
        slot.Hash.ShouldBe(expectedHash);
        slot.GracedUntil.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync returns InvalidCredentials when the email is not registered")]
    public async Task HandleAsync_ShouldReturnInvalidCredentials_WhenEmailNotFound()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var command = new LoginCommand
        {
            Email = "nobody@bowlneba.com",
            Password = LoginRequestFactory.ValidPassword
        };

        // Act
        var result = await CreateHandler(userManager, signInManager).HandleAsync(command, ct);

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
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        await SeedUserAsync(userManager);
        var command = new LoginCommand
        {
            Email = LoginRequestFactory.ValidEmail,
            Password = "WrongPassword9"
        };

        // Act
        var result = await CreateHandler(userManager, signInManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorOr.ErrorType.Unauthorized);
        result.FirstError.Code.ShouldBe("Login.InvalidCredentials");
    }
}