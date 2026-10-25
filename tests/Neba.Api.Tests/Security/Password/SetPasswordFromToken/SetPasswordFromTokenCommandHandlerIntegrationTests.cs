using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;

using Neba.Api.Contracts.Security;
using Neba.Api.Email;
using Neba.Api.Security;

using Neba.Api.Security.Domain;
using Neba.Api.Security.Password;
using Neba.Api.Security.Password.SetPasswordFromToken;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Security.Password.SetPasswordFromToken;

[IntegrationTest]
[Component("Security")]
[Collection<SecurityDbContextFixture>]
public sealed class SetPasswordFromTokenCommandHandlerIntegrationTests(SecurityDbContextFixture fixture)
    : IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
    private const string NewPassword = "NewPassword1!";

    private readonly FakeLogger<SetPasswordFromTokenCommandHandler> _logger = new();
    private readonly Mock<IEmailSender> _emailSender = new(MockBehavior.Strict);

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    private SetPasswordFromTokenCommandHandler CreateHandler(UserManager<ApplicationUser> userManager)
    {
        _emailSender
            .Setup(sender => sender.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var notifier = new PasswordChangedNotifier(
            _emailSender.Object,
            new WebsiteSettings { BaseUrl = "https://bowlneba.com" },
            NullLogger<PasswordChangedNotifier>.Instance);

        return new SetPasswordFromTokenCommandHandler(userManager, notifier, _logger);
    }

    private static async Task<ApplicationUser> SeedUserAsync(UserManager<ApplicationUser> userManager)
    {
        var user = ApplicationUserFactory.Create(userName: LoginRequestFactory.ValidEmail, email: LoginRequestFactory.ValidEmail);
        await userManager.CreateAsync(user, LoginRequestFactory.ValidPassword);
        return user;
    }

    [Fact(DisplayName = "HandleAsync returns InvalidOrExpiredToken when no user matches the given UserId")]
    public async Task HandleAsync_ShouldReturnInvalidOrExpiredToken_WhenUserDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var command = new SetPasswordFromTokenCommand
        {
            UserId = Ulid.NewUlid(),
            Token = "some-token",
            NewPassword = NewPassword
        };

        // Act
        var result = await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorOr.ErrorType.Validation);
        result.FirstError.Code.ShouldBe("Security.InvalidOrExpiredToken");
    }

    [Fact(DisplayName = "HandleAsync returns InvalidOrExpiredToken when the token is invalid")]
    public async Task HandleAsync_ShouldReturnInvalidOrExpiredToken_WhenTokenIsInvalid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        var command = new SetPasswordFromTokenCommand
        {
            UserId = user.Id,
            Token = "not-a-real-token",
            NewPassword = NewPassword
        };

        // Act
        var result = await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorOr.ErrorType.Validation);
        result.FirstError.Code.ShouldBe("Security.InvalidOrExpiredToken");
    }

    [Fact(DisplayName = "HandleAsync returns validation errors when the new password does not meet requirements")]
    public async Task HandleAsync_ShouldReturnValidationErrors_WhenNewPasswordIsWeak()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var command = new SetPasswordFromTokenCommand
        {
            UserId = user.Id,
            Token = token,
            NewPassword = "weak"
        };

        // Act
        var result = await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Errors.ShouldAllBe(error => error.Type == ErrorOr.ErrorType.Validation);
        result.Errors.ShouldAllBe(error => error.Code.StartsWith("SetPasswordFromToken.", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "HandleAsync returns Success when the token is valid")]
    public async Task HandleAsync_ShouldReturnSuccess_WhenTokenIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var command = new SetPasswordFromTokenCommand
        {
            UserId = user.Id,
            Token = token,
            NewPassword = NewPassword
        };

        // Act
        var result = await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync sets the new password so the user can log in with it")]
    public async Task HandleAsync_ShouldSetNewPassword_SoUserCanLogInWithIt()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var command = new SetPasswordFromTokenCommand
        {
            UserId = user.Id,
            Token = token,
            NewPassword = NewPassword
        };

        // Act
        await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        var freshUser = await userManager.FindByEmailAsync(LoginRequestFactory.ValidEmail);
        var canLoginWithNewPassword = await userManager.CheckPasswordAsync(freshUser!, NewPassword);
        canLoginWithNewPassword.ShouldBeTrue();
    }

    [Fact(DisplayName = "HandleAsync invalidates the original password after a successful reset")]
    public async Task HandleAsync_ShouldInvalidateOriginalPassword_AfterSuccessfulReset()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var command = new SetPasswordFromTokenCommand
        {
            UserId = user.Id,
            Token = token,
            NewPassword = NewPassword
        };

        // Act
        await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        var freshUser = await userManager.FindByEmailAsync(LoginRequestFactory.ValidEmail);
        var oldPasswordStillWorks = await userManager.CheckPasswordAsync(freshUser!, LoginRequestFactory.ValidPassword);
        oldPasswordStillWorks.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync confirms the user's email after a successful reset")]
    public async Task HandleAsync_ShouldConfirmEmail_AfterSuccessfulReset()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        user.EmailConfirmed.ShouldBeFalse();
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var command = new SetPasswordFromTokenCommand
        {
            UserId = user.Id,
            Token = token,
            NewPassword = NewPassword
        };

        // Act
        await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        var freshUser = await userManager.FindByEmailAsync(LoginRequestFactory.ValidEmail);
        freshUser!.EmailConfirmed.ShouldBeTrue();
    }

    [Fact(DisplayName = "HandleAsync does not confirm the user's email when the token is invalid")]
    public async Task HandleAsync_ShouldNotConfirmEmail_WhenTokenIsInvalid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        var command = new SetPasswordFromTokenCommand
        {
            UserId = user.Id,
            Token = "not-a-real-token",
            NewPassword = NewPassword
        };

        // Act
        await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        var freshUser = await userManager.FindByEmailAsync(LoginRequestFactory.ValidEmail);
        freshUser!.EmailConfirmed.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync removes the stored refresh token after a successful reset")]
    public async Task HandleAsync_ShouldRemoveRefreshToken_AfterSuccessfulReset()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        await RefreshTokenStore.StoreAsync(userManager, user, "raw-refresh-token", TimeProvider.System);
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var command = new SetPasswordFromTokenCommand { UserId = user.Id, Token = token, NewPassword = NewPassword };

        // Act
        await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        var freshUser = await userManager.FindByEmailAsync(LoginRequestFactory.ValidEmail);
        (await RefreshTokenStore.GetStoredJsonAsync(userManager, freshUser!)).ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync keeps the stored refresh token when the token is invalid")]
    public async Task HandleAsync_ShouldKeepRefreshToken_WhenTokenIsInvalid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        await RefreshTokenStore.StoreAsync(userManager, user, "raw-refresh-token", TimeProvider.System);
        var command = new SetPasswordFromTokenCommand { UserId = user.Id, Token = "not-a-real-token", NewPassword = NewPassword };

        // Act
        await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        var freshUser = await userManager.FindByEmailAsync(LoginRequestFactory.ValidEmail);
        (await RefreshTokenStore.GetStoredJsonAsync(userManager, freshUser!)).ShouldNotBeNull();
    }

    [Fact(DisplayName = "HandleAsync sends the password-changed notice when the email was already confirmed (a reset)")]
    public async Task HandleAsync_ShouldSendNotice_WhenEmailAlreadyConfirmed()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        user.EmailConfirmed = true;
        await userManager.UpdateAsync(user);
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var command = new SetPasswordFromTokenCommand { UserId = user.Id, Token = token, NewPassword = NewPassword };
        var handler = CreateHandler(userManager);

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        _emailSender.Verify(
            sender => sender.SendAsync(
                It.Is<EmailMessage>(m => m.To == LoginRequestFactory.ValidEmail && m.Subject == "Your BowlNEBA password was changed"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "HandleAsync sends no notice for a first-time password (an invite)")]
    public async Task HandleAsync_ShouldNotSendNotice_WhenEmailNotYetConfirmed()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var command = new SetPasswordFromTokenCommand { UserId = user.Id, Token = token, NewPassword = NewPassword };
        var handler = CreateHandler(userManager);

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        _emailSender.Verify(
            sender => sender.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "HandleAsync accepts an invite token when the purpose is invite")]
    public async Task HandleAsync_ShouldReturnSuccess_WhenInviteTokenUsedWithInvitePurpose()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        var token = await userManager.GenerateUserTokenAsync(
            user, InviteTokenProvider.ProviderName, UserManager<ApplicationUser>.ResetPasswordTokenPurpose);
        var command = new SetPasswordFromTokenCommand
        {
            UserId = user.Id,
            Token = token,
            NewPassword = NewPassword,
            Purpose = PasswordTokenPurpose.Invite
        };

        // Act
        var result = await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var freshUser = await userManager.FindByEmailAsync(LoginRequestFactory.ValidEmail);
        (await userManager.CheckPasswordAsync(freshUser!, NewPassword)).ShouldBeTrue();
        freshUser!.EmailConfirmed.ShouldBeTrue();
    }

    [Fact(DisplayName = "HandleAsync rejects an invite token when the purpose is reset")]
    public async Task HandleAsync_ShouldReturnInvalidOrExpiredToken_WhenInviteTokenUsedWithResetPurpose()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        var token = await userManager.GenerateUserTokenAsync(
            user, InviteTokenProvider.ProviderName, UserManager<ApplicationUser>.ResetPasswordTokenPurpose);
        var command = new SetPasswordFromTokenCommand
        {
            UserId = user.Id,
            Token = token,
            NewPassword = NewPassword,
            Purpose = PasswordTokenPurpose.Reset
        };

        // Act
        var result = await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Security.InvalidOrExpiredToken");
    }

    [Fact(DisplayName = "HandleAsync rejects a reset token when the purpose is invite")]
    public async Task HandleAsync_ShouldReturnInvalidOrExpiredToken_WhenResetTokenUsedWithInvitePurpose()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var command = new SetPasswordFromTokenCommand
        {
            UserId = user.Id,
            Token = token,
            NewPassword = NewPassword,
            Purpose = PasswordTokenPurpose.Invite
        };

        // Act
        var result = await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Security.InvalidOrExpiredToken");
    }
}
