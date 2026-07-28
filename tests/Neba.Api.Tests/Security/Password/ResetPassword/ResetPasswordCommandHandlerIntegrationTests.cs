using System.Net;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Email;
using Neba.Api.Security;
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
    private const string BaseUrl = "https://bowlneba.com";

    private readonly WebsiteSettings _websiteSettings = new() { BaseUrl = BaseUrl };

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    private ResetPasswordCommandHandler CreateHandler(
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender)
        => new(userManager, emailSender, _websiteSettings);

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
            .Returns(Task.CompletedTask);
        var command = new ResetPasswordCommand { UserId = user.Id };

        // Act
        await CreateHandler(userManager, emailSender.Object).HandleAsync(command, ct);

        // Assert
        sentMessage.ShouldNotBeNull();
        sentMessage.To.ShouldBe(RegisterRequestFactory.ValidEmail);
        sentMessage.Subject.ShouldBe("Your BowlNEBA password has been reset");
    }

    [Fact(DisplayName = "HandleAsync embeds a set-password link with the user's ID and a reset token in the email body")]
    public async Task HandleAsync_ShouldEmbedSetPasswordLink_InEmailBody()
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
        sentMessage.HtmlBody.ShouldContain($"{BaseUrl}/account/set-password?userId={user.Id}&amp;token=");
    }

    [Fact(DisplayName = "HandleAsync generates a token the user can redeem to set a new password")]
    public async Task HandleAsync_ShouldGenerateRedeemableToken_ForSettingNewPassword()
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
        var token = ExtractTokenFromLink(sentMessage.HtmlBody);
        var freshUser = await userManager.FindByEmailAsync(RegisterRequestFactory.ValidEmail);
        var resetResult = await userManager.ResetPasswordAsync(freshUser!, token, "SomeNewP@ssw0rd1");
        resetResult.Succeeded.ShouldBeTrue();
    }

    private static string ExtractTokenFromLink(string htmlBody)
    {
        const string marker = "&amp;token=";
        var start = htmlBody.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var end = htmlBody.IndexOfAny(['"', '<'], start);
        return WebUtility.UrlDecode(htmlBody[start..end]);
    }
}