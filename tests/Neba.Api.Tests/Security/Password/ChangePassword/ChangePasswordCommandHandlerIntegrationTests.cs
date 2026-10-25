using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Email;
using Neba.Api.Security;
using Neba.Api.Security.Domain;
using Neba.Api.Security.Password;
using Neba.Api.Security.Password.ChangePassword;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Security.Password.ChangePassword;

[IntegrationTest]
[Component("Security")]
[Collection<SecurityDbContextFixture>]
public sealed class ChangePasswordCommandHandlerIntegrationTests(SecurityDbContextFixture fixture)
    : IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
    private const string NewPassword = "NewPassword1!";

    private static readonly JwtSettings TestJwtSettings = new()
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        SigningKey = "test-signing-key-must-be-at-least-32-bytes-long!",
        AccessTokenExpiryMinutes = 15,
        RefreshTokenExpiryDays = 7
    };

    private readonly Mock<IEmailSender> _emailSender = new(MockBehavior.Strict);

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();

        _emailSender
            .Setup(sender => sender.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    private ChangePasswordCommandHandler CreateHandler(IServiceScope scope, IEmailSender? emailSender = null)
        => new(
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
            scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>(),
            fixture.CreateDbContext(),
            new JwtTokenService(TestJwtSettings, TimeProvider.System),
            TimeProvider.System,
            new PasswordChangedNotifier(
                emailSender ?? _emailSender.Object,
                new WebsiteSettings { BaseUrl = "https://bowlneba.com" },
                NullLogger<PasswordChangedNotifier>.Instance));

    private static async Task<ApplicationUser> SeedUserAsync(UserManager<ApplicationUser> userManager)
    {
        var user = ApplicationUserFactory.Create(userName: LoginRequestFactory.ValidEmail, email: LoginRequestFactory.ValidEmail);
        user.EmailConfirmed = true;
        await userManager.CreateAsync(user, LoginRequestFactory.ValidPassword);
        return user;
    }

    private static ChangePasswordCommand CreateCommand(Ulid userId, string currentPassword = LoginRequestFactory.ValidPassword)
        => new() { UserId = userId, CurrentPassword = currentPassword, NewPassword = NewPassword };

    [Fact(DisplayName = "HandleAsync returns UserNotFound when no user matches the ID")]
    public async Task HandleAsync_ShouldReturnUserNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();

        // Act
        var result = await CreateHandler(scope).HandleAsync(CreateCommand(Ulid.NewUlid()), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorOr.ErrorType.NotFound);
    }

    [Fact(DisplayName = "HandleAsync changes the password, returns tokens, and emails a notice when the current password is correct")]
    public async Task HandleAsync_ShouldChangePasswordAndReturnTokens_WhenCurrentPasswordIsCorrect()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);

        // Act
        var result = await CreateHandler(scope).HandleAsync(CreateCommand(user.Id), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.AccessToken.ShouldNotBeNullOrWhiteSpace();
        result.Value.RefreshToken.ShouldNotBeNullOrWhiteSpace();
        result.Value.UserId.ShouldBe(user.Id);
        result.Value.Email.ShouldBe(LoginRequestFactory.ValidEmail);

        var freshUser = await userManager.FindByIdAsync(user.Id.ToString());
        (await userManager.CheckPasswordAsync(freshUser!, NewPassword)).ShouldBeTrue();
        (await userManager.CheckPasswordAsync(freshUser!, LoginRequestFactory.ValidPassword)).ShouldBeFalse();

        _emailSender.Verify(
            sender => sender.SendAsync(
                It.Is<EmailMessage>(m => m.To == LoginRequestFactory.ValidEmail && m.Subject == "Your BowlNEBA password was changed"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "HandleAsync replaces the stored refresh token with the one it returns")]
    public async Task HandleAsync_ShouldReplaceStoredRefreshToken_WhenChangeSucceeds()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        await RefreshTokenStore.StoreAsync(userManager, user, "old-refresh-token", TimeProvider.System);

        // Act
        var result = await CreateHandler(scope).HandleAsync(CreateCommand(user.Id), ct);

        // Assert
        var stored = await RefreshTokenStore.GetStoredJsonAsync(userManager, (await userManager.FindByIdAsync(user.Id.ToString()))!);
        stored.ShouldNotBeNull();
        stored.ShouldContain(RefreshTokenStore.ComputeHash(result.Value.RefreshToken));
        stored.ShouldNotContain(RefreshTokenStore.ComputeHash("old-refresh-token"));
    }

    [Fact(DisplayName = "HandleAsync returns IncorrectCurrentPassword and changes nothing when the current password is wrong")]
    public async Task HandleAsync_ShouldReturnIncorrectCurrentPassword_WhenCurrentPasswordIsWrong()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        await RefreshTokenStore.StoreAsync(userManager, user, "old-refresh-token", TimeProvider.System);

        // Act
        var result = await CreateHandler(scope).HandleAsync(CreateCommand(user.Id, "WrongPassword1"), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorOr.ErrorType.Validation);
        result.FirstError.Code.ShouldBe("ChangePassword.IncorrectCurrentPassword");

        var freshUser = await userManager.FindByIdAsync(user.Id.ToString());
        (await userManager.CheckPasswordAsync(freshUser!, LoginRequestFactory.ValidPassword)).ShouldBeTrue();
        (await RefreshTokenStore.GetStoredJsonAsync(userManager, freshUser!))!.ShouldContain(RefreshTokenStore.ComputeHash("old-refresh-token"));
        _emailSender.Verify(sender => sender.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "HandleAsync locks the account after repeated wrong current passwords, even for the correct one")]
    public async Task HandleAsync_ShouldReturnLockedOut_AfterRepeatedWrongCurrentPasswords()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        var handler = CreateHandler(scope);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await handler.HandleAsync(CreateCommand(user.Id, "WrongPassword1"), ct);
        }

        // Act
        var result = await handler.HandleAsync(CreateCommand(user.Id), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorOr.ErrorType.Conflict);
        result.FirstError.Code.ShouldBe("ChangePassword.LockedOut");
        var freshUser = await userManager.FindByIdAsync(user.Id.ToString());
        (await userManager.CheckPasswordAsync(freshUser!, LoginRequestFactory.ValidPassword)).ShouldBeTrue();
    }

    [Fact(DisplayName = "HandleAsync returns validation errors when the new password breaks the password policy")]
    public async Task HandleAsync_ShouldReturnValidationErrors_WhenNewPasswordIsWeak()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        var command = new ChangePasswordCommand
        {
            UserId = user.Id,
            CurrentPassword = LoginRequestFactory.ValidPassword,
            NewPassword = "weak"
        };

        // Act
        var result = await CreateHandler(scope).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Errors.ShouldAllBe(error => error.Type == ErrorOr.ErrorType.Validation);
        result.Errors.ShouldAllBe(error => error.Code.StartsWith("ChangePassword.", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "HandleAsync still succeeds when the notice email fails to send")]
    public async Task HandleAsync_ShouldStillSucceed_WhenNoticeEmailFails()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        var failingSender = new Mock<IEmailSender>(MockBehavior.Strict);
        failingSender
            .Setup(sender => sender.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("smtp down"));

        // Act
        var result = await CreateHandler(scope, failingSender.Object).HandleAsync(CreateCommand(user.Id), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var freshUser = await userManager.FindByIdAsync(user.Id.ToString());
        (await userManager.CheckPasswordAsync(freshUser!, NewPassword)).ShouldBeTrue();
    }
}
