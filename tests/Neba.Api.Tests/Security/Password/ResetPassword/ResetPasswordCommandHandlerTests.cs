using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Email;
using Neba.Api.Security.Domain;
using Neba.Api.Security.Password.ResetPassword;
using Neba.Api.Security.Register;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Security.Password.ResetPassword;

[IntegrationTest]
[Component("Security")]
[Collection<SecurityDbContextFixture>]
public sealed class ResetPasswordCommandHandlerIntegrationTests(SecurityDbContextFixture fixture)
    : IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    private static ResetPasswordCommandHandler CreateHandler(
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender)
        => new(userManager, emailSender);

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

    [Fact(DisplayName = "HandleAsync returns UserNotFound when no user matches the given UserId")]
    public async Task HandleAsync_ShouldReturnUserNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var emailSender = new Mock<IEmailSender>(MockBehavior.Strict);
        var command = new ResetPasswordCommand { UserId = Ulid.NewUlid() };

        // Act
        var result = await CreateHandler(userManager, emailSender.Object).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorOr.ErrorType.NotFound);
        result.FirstError.Code.ShouldBe("Security.UserNotFound");
    }

    [Fact(DisplayName = "HandleAsync returns Success when the user exists")]
    public async Task HandleAsync_ShouldReturnSuccess_WhenUserExists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        var emailSender = new Mock<IEmailSender>(MockBehavior.Strict);
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var command = new ResetPasswordCommand { UserId = user.Id };

        // Act
        var result = await CreateHandler(userManager, emailSender.Object).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync sends the reset email to the user's email address")]
    public async Task HandleAsync_ShouldSendEmail_ToUserEmailAddress()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        EmailMessage? sentMessage = null;
        var emailSender = new Mock<IEmailSender>(MockBehavior.Strict);
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((msg, _) => sentMessage = msg)
            .Returns(Task.CompletedTask)
            .Verifiable();
        var command = new ResetPasswordCommand { UserId = user.Id };

        // Act
        await CreateHandler(userManager, emailSender.Object).HandleAsync(command, ct);

        // Assert
        emailSender.VerifyAll();
        sentMessage.ShouldNotBeNull();
        sentMessage!.To.ShouldBe(RegisterRequestFactory.ValidEmail);
    }

    [Fact(DisplayName = "HandleAsync embeds the generated temporary password in the email body")]
    public async Task HandleAsync_ShouldEmbedTempPassword_InEmailBody()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        EmailMessage? sentMessage = null;
        var emailSender = new Mock<IEmailSender>(MockBehavior.Strict);
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((msg, _) => sentMessage = msg)
            .Returns(Task.CompletedTask);
        var command = new ResetPasswordCommand { UserId = user.Id };

        // Act
        await CreateHandler(userManager, emailSender.Object).HandleAsync(command, ct);

        // Assert
        sentMessage.ShouldNotBeNull();
        sentMessage!.HtmlBody.ShouldNotContain("{tempPassword}");
        var tempPassword = ExtractTempPasswordFromBody(sentMessage.HtmlBody);
        var freshUser = await userManager.FindByEmailAsync(RegisterRequestFactory.ValidEmail);
        var canLogin = await userManager.CheckPasswordAsync(freshUser!, tempPassword);
        canLogin.ShouldBeTrue();
    }

    [Fact(DisplayName = "HandleAsync invalidates the original password after reset")]
    public async Task HandleAsync_ShouldInvalidateOriginalPassword_AfterReset()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        var emailSender = new Mock<IEmailSender>(MockBehavior.Strict);
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var command = new ResetPasswordCommand { UserId = user.Id };

        // Act
        await CreateHandler(userManager, emailSender.Object).HandleAsync(command, ct);

        // Assert
        var freshUser = await userManager.FindByEmailAsync(RegisterRequestFactory.ValidEmail);
        var oldPasswordStillWorks = await userManager.CheckPasswordAsync(freshUser!, RegisterRequestFactory.ValidPassword);
        oldPasswordStillWorks.ShouldBeFalse();
    }

    private static string ExtractTempPasswordFromBody(string htmlBody)
    {
        const string open = "<strong>";
        const string close = "</strong>";
        var start = htmlBody.IndexOf(open, StringComparison.Ordinal) + open.Length;
        var end = htmlBody.IndexOf(close, start, StringComparison.Ordinal);
        return htmlBody[start..end];
    }
}
