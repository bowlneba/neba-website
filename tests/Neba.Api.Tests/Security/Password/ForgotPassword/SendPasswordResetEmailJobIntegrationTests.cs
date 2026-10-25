using System.Net;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Email;
using Neba.Api.Security;
using Neba.Api.Security.Domain;
using Neba.Api.Security.Password.ForgotPassword;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Security.Password.ForgotPassword;

[IntegrationTest]
[Component("Security")]
[Collection<SecurityDbContextFixture>]
public sealed class SendPasswordResetEmailJobIntegrationTests(SecurityDbContextFixture fixture)
    : IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
    private const string BaseUrl = "https://bowlneba.com";

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    private static async Task<ApplicationUser> SeedUserAsync(UserManager<ApplicationUser> userManager)
    {
        var user = ApplicationUserFactory.Create(userName: LoginRequestFactory.ValidEmail, email: LoginRequestFactory.ValidEmail);
        await userManager.CreateAsync(user, LoginRequestFactory.ValidPassword);
        return user;
    }

    [Fact(DisplayName = "SendAsync emails a reset link with reason=reset and a token that resets the password")]
    public async Task SendAsync_ShouldEmailRedeemableResetLink_WhenUserExists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        EmailMessage? sent = null;
        var emailSender = new Mock<IEmailSender>(MockBehavior.Strict);
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((message, _) => sent = message)
            .Returns(Task.CompletedTask);
        var job = new SendPasswordResetEmailJob(userManager, emailSender.Object, new WebsiteSettings { BaseUrl = BaseUrl });

        // Act
        await job.SendAsync(user.Id.ToString(), ct);

        // Assert
        sent.ShouldNotBeNull();
        sent.To.ShouldBe(LoginRequestFactory.ValidEmail);
        sent.Subject.ShouldBe("Reset your BowlNEBA password");
        sent.HtmlBody.ShouldContain($"{BaseUrl}/account/set-password?userId={user.Id}&amp;token=");
        sent.HtmlBody.ShouldContain("&amp;reason=reset");

        var marker = "&amp;token=";
        var start = sent.HtmlBody.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var end = sent.HtmlBody.IndexOf('&', start);
        var token = WebUtility.UrlDecode(sent.HtmlBody[start..end]);
        var freshUser = await userManager.FindByIdAsync(user.Id.ToString());
        (await userManager.VerifyUserTokenAsync(
            freshUser!, ResetTokenProvider.ProviderName, UserManager<ApplicationUser>.ResetPasswordTokenPurpose, token))
            .ShouldBeTrue();
    }

    [Fact(DisplayName = "SendAsync sends nothing when the user no longer exists")]
    public async Task SendAsync_ShouldSendNothing_WhenUserDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var emailSender = new Mock<IEmailSender>(MockBehavior.Strict);
        var job = new SendPasswordResetEmailJob(userManager, emailSender.Object, new WebsiteSettings { BaseUrl = BaseUrl });

        // Act
        await job.SendAsync(Ulid.NewUlid().ToString(), ct);

        // Assert
        emailSender.VerifyNoOtherCalls();
    }
}
