using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;

using Neba.Api.Security.Domain;
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

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    private SetPasswordFromTokenCommandHandler CreateHandler(UserManager<ApplicationUser> userManager)
        => new(userManager, _logger);

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
}