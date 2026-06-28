using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

using Neba.Api.Security;
using Neba.Api.Security.Domain;
using Neba.Api.Security.Login;
using Neba.Api.Security.RefreshToken;
using Neba.Api.Security.Register;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Security.RefreshToken;

[IntegrationTest]
[Component("Security")]
[Collection<SecurityDbContextFixture>]
public sealed class RefreshTokenCommandHandlerIntegrationTests(SecurityDbContextFixture fixture)
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

    private static RefreshTokenCommandHandler CreateHandler(
        UserManager<ApplicationUser> userManager,
        TimeProvider? timeProvider = null,
        ILogger<RefreshTokenCommandHandler>? logger = null)
        => new(
            userManager,
            new JwtTokenService(TestJwtSettings, timeProvider ?? TimeProvider.System),
            TestJwtSettings,
            timeProvider ?? TimeProvider.System,
            logger ?? NullLogger<RefreshTokenCommandHandler>.Instance);

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
        UserManager<ApplicationUser> userManager,
        TimeProvider? timeProvider = null)
    {
        var user = await SeedUserAsync(userManager);
        var tp = timeProvider ?? TimeProvider.System;
        var loginResult = await new LoginCommandHandler(
                userManager,
                new JwtTokenService(TestJwtSettings, tp),
                tp)
            .HandleAsync(
                new LoginCommand
                {
                    Email = RegisterRequestFactory.ValidEmail,
                    Password = RegisterRequestFactory.ValidPassword
                },
                CancellationToken.None);
        return (user, loginResult.Value.RefreshToken);
    }

    [Fact(DisplayName = "HandleAsync returns InvalidRefreshToken when the user does not exist")]
    public async Task HandleAsync_ShouldReturnInvalidRefreshToken_WhenUserDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var command = new RefreshTokenCommand
        {
            UserId = Ulid.NewUlid(),
            RefreshToken = "some-token"
        };

        // Act
        var result = await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorOr.ErrorType.Unauthorized);
        result.FirstError.Code.ShouldBe("RefreshToken.InvalidRefreshToken");
    }

    [Fact(DisplayName = "HandleAsync returns InvalidRefreshToken when no refresh token is stored for the user")]
    public async Task HandleAsync_ShouldReturnInvalidRefreshToken_WhenNoStoredToken()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        var command = new RefreshTokenCommand
        {
            UserId = user.Id,
            RefreshToken = "some-token"
        };

        // Act
        var result = await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorOr.ErrorType.Unauthorized);
        result.FirstError.Code.ShouldBe("RefreshToken.InvalidRefreshToken");
    }

    [Fact(DisplayName = "HandleAsync returns InvalidRefreshToken and logs error when stored token JSON is corrupt")]
    public async Task HandleAsync_ShouldReturnInvalidRefreshToken_AndLogError_WhenStoredTokenIsCorrupt()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        await userManager.SetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName, "not-valid-json{{{");

        var logger = new FakeLogger<RefreshTokenCommandHandler>();
        var command = new RefreshTokenCommand
        {
            UserId = user.Id,
            RefreshToken = "some-token"
        };

        // Act
        var result = await CreateHandler(userManager, logger: logger).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("RefreshToken.InvalidRefreshToken");

        var logEntry = logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        logEntry.Level.ShouldBe(LogLevel.Error);
        logEntry.Message.ShouldContain(user.Id.ToString());
    }

    [Fact(DisplayName = "HandleAsync returns InvalidRefreshToken when the token hash does not match")]
    public async Task HandleAsync_ShouldReturnInvalidRefreshToken_WhenTokenHashDoesNotMatch()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var (user, _) = await SeedLoginAsync(userManager);
        var command = new RefreshTokenCommand
        {
            UserId = user.Id,
            RefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
        };

        // Act
        var result = await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorOr.ErrorType.Unauthorized);
        result.FirstError.Code.ShouldBe("RefreshToken.InvalidRefreshToken");
    }

    [Fact(DisplayName = "HandleAsync returns InvalidRefreshToken when the refresh token has expired")]
    public async Task HandleAsync_ShouldReturnInvalidRefreshToken_WhenTokenIsExpired()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var issuedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var seedTimeProvider = new FakeTimeProvider(issuedAt);
        var (user, refreshToken) = await SeedLoginAsync(userManager, seedTimeProvider);

        var expiredTimeProvider = new FakeTimeProvider(issuedAt.AddDays(TestJwtSettings.RefreshTokenExpiryDays + 1));
        var command = new RefreshTokenCommand
        {
            UserId = user.Id,
            RefreshToken = refreshToken
        };

        // Act
        var result = await CreateHandler(userManager, expiredTimeProvider).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorOr.ErrorType.Unauthorized);
        result.FirstError.Code.ShouldBe("RefreshToken.InvalidRefreshToken");
    }

    [Fact(DisplayName = "HandleAsync returns LoginDto with non-empty tokens when the refresh token is valid")]
    public async Task HandleAsync_ShouldReturnLoginDto_WhenRefreshTokenIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var (user, refreshToken) = await SeedLoginAsync(userManager);
        var command = new RefreshTokenCommand
        {
            UserId = user.Id,
            RefreshToken = refreshToken
        };

        // Act
        var result = await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.AccessToken.ShouldNotBeNullOrEmpty();
        result.Value.RefreshToken.ShouldNotBeNullOrEmpty();
        result.Value.ExpiresAt.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
    }

    [Fact(DisplayName = "HandleAsync returns LoginDto with correct email and userId when the refresh token is valid")]
    public async Task HandleAsync_ShouldReturnLoginDto_WithCorrectEmailAndUserId_WhenRefreshTokenIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var (user, refreshToken) = await SeedLoginAsync(userManager);
        var command = new RefreshTokenCommand
        {
            UserId = user.Id,
            RefreshToken = refreshToken
        };

        // Act
        var result = await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Email.ShouldBe(RegisterRequestFactory.ValidEmail);
        result.Value.UserId.ShouldBe(user.Id);
    }

    [Fact(DisplayName = "HandleAsync stores a new hashed refresh token after successfully refreshing")]
    public async Task HandleAsync_ShouldStoreNewHashedRefreshToken_AfterSuccessfulRefresh()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var (user, oldRefreshToken) = await SeedLoginAsync(userManager);
        var command = new RefreshTokenCommand
        {
            UserId = user.Id,
            RefreshToken = oldRefreshToken
        };

        // Act
        var result = await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();

        var storedJson = await userManager.GetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName);
        storedJson.ShouldNotBeNullOrEmpty();

        var stored = JsonSerializer.Deserialize<StoredRefreshToken>(storedJson!);
        stored.ShouldNotBeNull();

        var expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(result.Value.RefreshToken)));
        stored!.Hash.ShouldBe(expectedHash);
        stored.Hash.ShouldNotBe(
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(oldRefreshToken))),
            "new token should replace the old one");
    }
}