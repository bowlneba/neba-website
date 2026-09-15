using System.Security.Cryptography;
using System.Text.Json;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

using Neba.Api.Database;
using Neba.Api.Security;
using Neba.Api.Security.Domain;
using Neba.Api.Security.Login;
using Neba.Api.Security.RefreshToken;
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
        RoleManager<ApplicationRole> roleManager,
        SecurityDbContext securityDbContext,
        TimeProvider? timeProvider = null,
        ILogger<RefreshTokenCommandHandler>? logger = null)
        => new(
            userManager,
            roleManager,
            securityDbContext,
            new JwtTokenService(TestJwtSettings, timeProvider ?? TimeProvider.System),
            TestJwtSettings,
            timeProvider ?? TimeProvider.System,
            logger ?? NullLogger<RefreshTokenCommandHandler>.Instance);

    private static async Task<ApplicationUser> SeedUserAsync(UserManager<ApplicationUser> userManager)
    {
        var user = ApplicationUserFactory.Create(userName: LoginRequestFactory.ValidEmail, email: LoginRequestFactory.ValidEmail);
        user.EmailConfirmed = true;
        await userManager.CreateAsync(user, LoginRequestFactory.ValidPassword);
        return user;
    }

    private static async Task<(ApplicationUser User, string RefreshToken)> SeedLoginAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        SignInManager<ApplicationUser> signInManager,
        TimeProvider? timeProvider = null)
    {
        var user = await SeedUserAsync(userManager);
        var tp = timeProvider ?? TimeProvider.System;
        var loginResult = await new LoginCommandHandler(
                userManager,
                roleManager,
                signInManager,
                new JwtTokenService(TestJwtSettings, tp),
                tp)
            .HandleAsync(
                new LoginCommand
                {
                    Email = LoginRequestFactory.ValidEmail,
                    Password = LoginRequestFactory.ValidPassword
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
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var securityDbContext = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
        var command = new RefreshTokenCommand
        {
            UserId = Ulid.NewUlid(),
            RefreshToken = "some-token"
        };

        // Act
        var result = await CreateHandler(userManager, roleManager, securityDbContext).HandleAsync(command, ct);

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
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var securityDbContext = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
        var user = await SeedUserAsync(userManager);
        var command = new RefreshTokenCommand
        {
            UserId = user.Id,
            RefreshToken = "some-token"
        };

        // Act
        var result = await CreateHandler(userManager, roleManager, securityDbContext).HandleAsync(command, ct);

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
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var securityDbContext = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
        var user = await SeedUserAsync(userManager);
        await userManager.SetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName, "not-valid-json{{{");

        var logger = new FakeLogger<RefreshTokenCommandHandler>();
        var command = new RefreshTokenCommand
        {
            UserId = user.Id,
            RefreshToken = "some-token"
        };

        // Act
        var result = await CreateHandler(userManager, roleManager, securityDbContext, logger: logger).HandleAsync(command, ct);

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
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var securityDbContext = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var (user, _) = await SeedLoginAsync(userManager, roleManager, signInManager);
        var command = new RefreshTokenCommand
        {
            UserId = user.Id,
            RefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
        };

        // Act
        var result = await CreateHandler(userManager, roleManager, securityDbContext).HandleAsync(command, ct);

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
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var securityDbContext = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
        var issuedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var seedTimeProvider = new FakeTimeProvider(issuedAt);
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var (user, refreshToken) = await SeedLoginAsync(userManager, roleManager, signInManager, seedTimeProvider);

        var expiredTimeProvider = new FakeTimeProvider(issuedAt.AddDays(TestJwtSettings.RefreshTokenExpiryDays + 1));
        var command = new RefreshTokenCommand
        {
            UserId = user.Id,
            RefreshToken = refreshToken
        };

        // Act
        var result = await CreateHandler(userManager, roleManager, securityDbContext, expiredTimeProvider).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorOr.ErrorType.Unauthorized);
        result.FirstError.Code.ShouldBe("RefreshToken.InvalidRefreshToken");
    }

    [Fact(DisplayName = "HandleAsync returns RefreshTokenDto with non-empty tokens when the refresh token is valid")]
    public async Task HandleAsync_ShouldReturnRefreshTokenDto_WhenRefreshTokenIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var securityDbContext = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var (user, refreshToken) = await SeedLoginAsync(userManager, roleManager, signInManager);
        var command = new RefreshTokenCommand
        {
            UserId = user.Id,
            RefreshToken = refreshToken
        };

        // Act
        var result = await CreateHandler(userManager, roleManager, securityDbContext).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.AccessToken.ShouldNotBeNullOrEmpty();
        result.Value.RefreshToken.ShouldNotBeNullOrEmpty();
        result.Value.ExpiresAt.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
    }

    [Fact(DisplayName = "HandleAsync returns RefreshTokenDto with correct email and userId when the refresh token is valid")]
    public async Task HandleAsync_ShouldReturnRefreshTokenDto_WithCorrectEmailAndUserId_WhenRefreshTokenIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var securityDbContext = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var (user, refreshToken) = await SeedLoginAsync(userManager, roleManager, signInManager);
        var command = new RefreshTokenCommand
        {
            UserId = user.Id,
            RefreshToken = refreshToken
        };

        // Act
        var result = await CreateHandler(userManager, roleManager, securityDbContext).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Email.ShouldBe(LoginRequestFactory.ValidEmail);
        result.Value.UserId.ShouldBe(user.Id);
    }

    [Fact(DisplayName = "HandleAsync stores a new fully-valid slot and demotes the old one to a grace slot after successfully refreshing")]
    public async Task HandleAsync_ShouldStoreNewSlotAndDemoteOldOne_AfterSuccessfulRefresh()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var securityDbContext = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var (user, oldRefreshToken) = await SeedLoginAsync(userManager, roleManager, signInManager);
        var command = new RefreshTokenCommand
        {
            UserId = user.Id,
            RefreshToken = oldRefreshToken
        };

        // Act
        var result = await CreateHandler(userManager, roleManager, securityDbContext).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();

        var storedJson = await userManager.GetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName);
        storedJson.ShouldNotBeNullOrEmpty();

        var stored = JsonSerializer.Deserialize<StoredRefreshToken>(storedJson);
        stored.ShouldNotBeNull();

        var newHash = RefreshTokenStore.ComputeHash(result.Value.RefreshToken);
        var oldHash = RefreshTokenStore.ComputeHash(oldRefreshToken);

        var newSlot = stored.Slots.SingleOrDefault(s => s.Hash == newHash);
        newSlot.ShouldNotBeNull("the newly issued token must be stored as a fully valid slot");
        newSlot.GracedUntil.ShouldBeNull();

        var oldSlot = stored.Slots.SingleOrDefault(s => s.Hash == oldHash);
        oldSlot.ShouldNotBeNull("the just-superseded token must still be present as a grace slot");
        oldSlot.GracedUntil.ShouldNotBeNull();
    }

    [Fact(DisplayName = "HandleAsync succeeds when presented with the immediately-prior refresh token within the grace window")]
    public async Task HandleAsync_ShouldSucceed_WhenPresentedWithPreviousTokenWithinGraceWindow()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var securityDbContext = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var (user, firstRefreshToken) = await SeedLoginAsync(userManager, roleManager, signInManager, timeProvider);

        // A first, winning refresh rotates the stored token out from under firstRefreshToken.
        await CreateHandler(userManager, roleManager, securityDbContext, timeProvider)
            .HandleAsync(new RefreshTokenCommand { UserId = user.Id, RefreshToken = firstRefreshToken }, ct);
        timeProvider.Advance(TimeSpan.FromSeconds(5));

        // Act — a request that raced in with the now-rotated-out token, still inside the grace window.
        var result = await CreateHandler(userManager, roleManager, securityDbContext, timeProvider)
            .HandleAsync(new RefreshTokenCommand { UserId = user.Id, RefreshToken = firstRefreshToken }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.AccessToken.ShouldNotBeNullOrEmpty();
        result.Value.RefreshToken.ShouldNotBeNullOrEmpty();
    }

    [Fact(DisplayName = "HandleAsync returns InvalidRefreshToken when presented with the immediately-prior refresh token after the grace window has passed")]
    public async Task HandleAsync_ShouldReturnInvalidRefreshToken_WhenPresentedWithPreviousTokenAfterGraceWindow()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var securityDbContext = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var (user, firstRefreshToken) = await SeedLoginAsync(userManager, roleManager, signInManager, timeProvider);

        await CreateHandler(userManager, roleManager, securityDbContext, timeProvider)
            .HandleAsync(new RefreshTokenCommand { UserId = user.Id, RefreshToken = firstRefreshToken }, ct);
        timeProvider.Advance(TimeSpan.FromSeconds(31));

        // Act
        var result = await CreateHandler(userManager, roleManager, securityDbContext, timeProvider)
            .HandleAsync(new RefreshTokenCommand { UserId = user.Id, RefreshToken = firstRefreshToken }, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorOr.ErrorType.Unauthorized);
        result.FirstError.Code.ShouldBe("RefreshToken.InvalidRefreshToken");
    }

    [Fact(DisplayName = "HandleAsync keeps the same grace slot when a racing request rotates again, so a second racer with the original token still succeeds")]
    public async Task HandleAsync_ShouldPreserveGraceSlot_WhenRacingRequestRotatesAgain()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var securityDbContext = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var (user, firstRefreshToken) = await SeedLoginAsync(userManager, roleManager, signInManager, timeProvider);

        // Winner rotates first, then a racer presenting the same original token rotates again.
        await CreateHandler(userManager, roleManager, securityDbContext, timeProvider)
            .HandleAsync(new RefreshTokenCommand { UserId = user.Id, RefreshToken = firstRefreshToken }, ct);
        await CreateHandler(userManager, roleManager, securityDbContext, timeProvider)
            .HandleAsync(new RefreshTokenCommand { UserId = user.Id, RefreshToken = firstRefreshToken }, ct);

        // Act — a second racer, also presenting the same original token.
        var result = await CreateHandler(userManager, roleManager, securityDbContext, timeProvider)
            .HandleAsync(new RefreshTokenCommand { UserId = user.Id, RefreshToken = firstRefreshToken }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.AccessToken.ShouldNotBeNullOrEmpty();
        result.Value.RefreshToken.ShouldNotBeNullOrEmpty();
    }

    [Fact(DisplayName = "HandleAsync does not invalidate the legitimate rotated-forward token when a racer replays the previous token within the grace window")]
    public async Task HandleAsync_ShouldNotEvictRotatedForwardToken_WhenRacerReplaysPreviousTokenWithinGraceWindow()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var securityDbContext = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var (user, firstRefreshToken) = await SeedLoginAsync(userManager, roleManager, signInManager, timeProvider);

        // The legitimate rotation: firstRefreshToken -> rotatedForwardToken.
        var rotationResult = await CreateHandler(userManager, roleManager, securityDbContext, timeProvider)
            .HandleAsync(new RefreshTokenCommand { UserId = user.Id, RefreshToken = firstRefreshToken }, ct);
        var rotatedForwardToken = rotationResult.Value.RefreshToken;
        timeProvider.Advance(TimeSpan.FromSeconds(5));

        // A racer replays the now-superseded firstRefreshToken, still inside the grace window.
        var racerResult = await CreateHandler(userManager, roleManager, securityDbContext, timeProvider)
            .HandleAsync(new RefreshTokenCommand { UserId = user.Id, RefreshToken = firstRefreshToken }, ct);
        racerResult.IsError.ShouldBeFalse();

        // Act — the legitimate client presents the token it actually rotated forward to.
        var result = await CreateHandler(userManager, roleManager, securityDbContext, timeProvider)
            .HandleAsync(new RefreshTokenCommand { UserId = user.Id, RefreshToken = rotatedForwardToken }, ct);

        // Assert
        result.IsError.ShouldBeFalse("the racer's grace-window replay must not evict the legitimately rotated-forward token");
        result.Value.AccessToken.ShouldNotBeNullOrEmpty();
        result.Value.RefreshToken.ShouldNotBeNullOrEmpty();
    }

    [Fact(DisplayName = "HandleAsync succeeds when a racer's grace-window-issued refresh token is reused much later")]
    public async Task HandleAsync_ShouldSucceed_WhenRacerGraceTokenIsReusedLater()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var securityDbContext = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var (user, firstRefreshToken) = await SeedLoginAsync(userManager, roleManager, signInManager, timeProvider);

        await CreateHandler(userManager, roleManager, securityDbContext, timeProvider)
            .HandleAsync(new RefreshTokenCommand { UserId = user.Id, RefreshToken = firstRefreshToken }, ct);
        timeProvider.Advance(TimeSpan.FromSeconds(5));

        var racerResult = await CreateHandler(userManager, roleManager, securityDbContext, timeProvider)
            .HandleAsync(new RefreshTokenCommand { UserId = user.Id, RefreshToken = firstRefreshToken }, ct);
        racerResult.IsError.ShouldBeFalse();

        // Advance well past the original 30s grace window and a normal access-token lifetime -
        // exactly the scenario a dead-end bridge token used to fail: a legitimate client retrying
        // after a lost response, then trying to refresh again much later.
        timeProvider.Advance(TimeSpan.FromMinutes(15));

        // Act — the racer's own reissued refresh token, presented long after the original race.
        var result = await CreateHandler(userManager, roleManager, securityDbContext, timeProvider)
            .HandleAsync(new RefreshTokenCommand { UserId = user.Id, RefreshToken = racerResult.Value.RefreshToken }, ct);

        // Assert
        result.IsError.ShouldBeFalse("a racer-issued token must be just as durable as any other - no dead-end bridge tokens");
        result.Value.AccessToken.ShouldNotBeNullOrEmpty();
        result.Value.RefreshToken.ShouldNotBeNullOrEmpty();
    }

    [Fact(DisplayName = "HandleAsync leaves the legitimately rotated-forward slot untouched when a racer replays the previous token within the grace window")]
    public async Task HandleAsync_ShouldLeaveWinnerSlotUntouched_WhenRacerReplaysPreviousTokenWithinGraceWindow()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var securityDbContext = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var (user, firstRefreshToken) = await SeedLoginAsync(userManager, roleManager, signInManager, timeProvider);

        var rotationResult = await CreateHandler(userManager, roleManager, securityDbContext, timeProvider)
            .HandleAsync(new RefreshTokenCommand { UserId = user.Id, RefreshToken = firstRefreshToken }, ct);
        var rotatedForwardToken = rotationResult.Value.RefreshToken;
        timeProvider.Advance(TimeSpan.FromSeconds(5));

        // Act — a racer replays the now-superseded firstRefreshToken, still inside the grace window.
        var racerResult = await CreateHandler(userManager, roleManager, securityDbContext, timeProvider)
            .HandleAsync(new RefreshTokenCommand { UserId = user.Id, RefreshToken = firstRefreshToken }, ct);

        // Assert
        racerResult.IsError.ShouldBeFalse();
        var storedJson = await userManager.GetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName);
        var stored = JsonSerializer.Deserialize<StoredRefreshToken>(storedJson!);
        stored.ShouldNotBeNull();

        var winnerHash = RefreshTokenStore.ComputeHash(rotatedForwardToken);
        var winnerSlot = stored.Slots.SingleOrDefault(s => s.Hash == winnerHash);
        winnerSlot.ShouldNotBeNull("a grace-window racer must not affect a sibling caller's own valid slot");
        winnerSlot.GracedUntil.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync logs an informational graced-slot-promoted event when a racer replays the previous token within the grace window")]
    public async Task HandleAsync_ShouldLogGracedSlotPromoted_WhenRacerReplaysPreviousTokenWithinGraceWindow()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var securityDbContext = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var (user, firstRefreshToken) = await SeedLoginAsync(userManager, roleManager, signInManager, timeProvider);

        await CreateHandler(userManager, roleManager, securityDbContext, timeProvider)
            .HandleAsync(new RefreshTokenCommand { UserId = user.Id, RefreshToken = firstRefreshToken }, ct);
        timeProvider.Advance(TimeSpan.FromSeconds(5));

        var logger = new FakeLogger<RefreshTokenCommandHandler>();

        // Act — a racer replays the now-superseded firstRefreshToken, still inside the grace window.
        var racerResult = await CreateHandler(userManager, roleManager, securityDbContext, timeProvider, logger: logger)
            .HandleAsync(new RefreshTokenCommand { UserId = user.Id, RefreshToken = firstRefreshToken }, ct);

        // Assert
        racerResult.IsError.ShouldBeFalse();
        var logEntry = logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        logEntry.Level.ShouldBe(LogLevel.Information);
        logEntry.Message.ShouldContain(user.Id.ToString());
    }

    [Fact(DisplayName = "HandleAsync succeeds when presented with the previous token exactly at the grace window boundary")]
    public async Task HandleAsync_ShouldSucceed_WhenPresentedWithPreviousTokenExactlyAtGraceWindowBoundary()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var securityDbContext = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var (user, firstRefreshToken) = await SeedLoginAsync(userManager, roleManager, signInManager, timeProvider);

        await CreateHandler(userManager, roleManager, securityDbContext, timeProvider)
            .HandleAsync(new RefreshTokenCommand { UserId = user.Id, RefreshToken = firstRefreshToken }, ct);
        timeProvider.Advance(TimeSpan.FromSeconds(30)); // exactly the RotationGraceWindow

        // Act
        var result = await CreateHandler(userManager, roleManager, securityDbContext, timeProvider)
            .HandleAsync(new RefreshTokenCommand { UserId = user.Id, RefreshToken = firstRefreshToken }, ct);

        // Assert
        result.IsError.ShouldBeFalse("the grace window boundary is inclusive (now <= GracedUntil)");
    }

    [Fact(DisplayName = "HandleAsync leaves neither caller with a dead-end token when two requests concurrently present the same still-current refresh token")]
    public async Task HandleAsync_ShouldLeaveNeitherCallerWithADeadEndToken_WhenTwoRequestsConcurrentlyPresentSameCurrentToken()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scopeA = fixture.CreateScope();
        using var scopeB = fixture.CreateScope();
        var userManagerA = scopeA.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManagerA = scopeA.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var securityDbContextA = scopeA.ServiceProvider.GetRequiredService<SecurityDbContext>();
        var signInManagerA = scopeA.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var userManagerB = scopeB.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManagerB = scopeB.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var securityDbContextB = scopeB.ServiceProvider.GetRequiredService<SecurityDbContext>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var (user, currentToken) = await SeedLoginAsync(userManagerA, roleManagerA, signInManagerA, timeProvider);

        // Act — two requests, each in its own DI scope/DbContext (mirroring two separate HTTP
        // requests), race presenting the same still-current token - e.g. two tabs whose access
        // tokens expire together.
        var taskA = CreateHandler(userManagerA, roleManagerA, securityDbContextA, timeProvider)
            .HandleAsync(new RefreshTokenCommand { UserId = user.Id, RefreshToken = currentToken }, ct);
        var taskB = CreateHandler(userManagerB, roleManagerB, securityDbContextB, timeProvider)
            .HandleAsync(new RefreshTokenCommand { UserId = user.Id, RefreshToken = currentToken }, ct);
        var results = await Task.WhenAll(taskA, taskB);

        // Assert — neither caller is rejected outright by the race itself.
        results[0].IsError.ShouldBeFalse();
        results[1].IsError.ShouldBeFalse();

        // Both reissued tokens - the CAS winner's own new token, and the CAS loser's token
        // (promoted from what became a grace slot) - must remain independently usable afterward,
        // each from a fresh scope like a real follow-up request. Neither is a dead-end bridge, and
        // redeeming one must not affect the other's own slot.
        using var verifyScope = fixture.CreateScope();
        var verifyUserManager = verifyScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var verifyRoleManager = verifyScope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var verifySecurityDbContext = verifyScope.ServiceProvider.GetRequiredService<SecurityDbContext>();

        var followUpA = await CreateHandler(verifyUserManager, verifyRoleManager, verifySecurityDbContext, timeProvider)
            .HandleAsync(new RefreshTokenCommand { UserId = user.Id, RefreshToken = results[0].Value.RefreshToken }, ct);
        followUpA.IsError.ShouldBeFalse("the first racer's reissued token must remain usable, not a dead-end bridge");

        var followUpB = await CreateHandler(verifyUserManager, verifyRoleManager, verifySecurityDbContext, timeProvider)
            .HandleAsync(new RefreshTokenCommand { UserId = user.Id, RefreshToken = results[1].Value.RefreshToken }, ct);
        followUpB.IsError.ShouldBeFalse(
            "the second racer's reissued token must remain usable even after the first racer's own follow-up redemption - a sibling slot must never be affected by someone else's redemption");
    }
}