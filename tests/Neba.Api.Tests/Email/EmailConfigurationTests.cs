using MailKit.Security;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Neba.Api.Email;
using Neba.Api.Security.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Email;

[IntegrationTest]
[Component("Email")]
public sealed class EmailConfigurationTests
{
    [Fact(DisplayName = "AddEmail should register GoogleWorkspaceEmailSender as IEmailSender")]
    public void AddEmail_ShouldRegisterGoogleWorkspaceEmailSender()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();

        // Act
        builder.AddEmail();

        // Assert
        builder.Services.ShouldContain(sd =>
            sd.ServiceType == typeof(IEmailSender) &&
            sd.ImplementationType == typeof(GoogleWorkspaceEmailSender));
    }

    [Fact(DisplayName = "AddEmail should register IdentityEmailSenderAdapter as IEmailSender<ApplicationUser>")]
    public void AddEmail_ShouldRegisterIdentityEmailSenderAdapter()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();

        // Act
        builder.AddEmail();

        // Assert
        builder.Services.ShouldContain(sd =>
            sd.ServiceType == typeof(Microsoft.AspNetCore.Identity.IEmailSender<ApplicationUser>) &&
            sd.ImplementationType == typeof(IdentityEmailSenderAdapter));
    }

    [Fact(DisplayName = "AddEmail should use default SMTP settings when mailpit is not configured")]
    public void AddEmail_ShouldUseDefaultSmtpSettings_WhenMailpitNotConfigured()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();

        // Act
        builder.AddEmail();

        // Assert
        var settings = builder.Services.BuildServiceProvider()
            .GetRequiredService<IOptions<EmailSettings>>().Value;
        settings.Host.ShouldBe("smtp.gmail.com");
        settings.TlsMode.ShouldBe(SecureSocketOptions.StartTls);
    }

    [Fact(DisplayName = "AddEmail should override SMTP settings with mailpit endpoint when mailpit connection string is present")]
    public void AddEmail_ShouldOverrideSmtpSettings_WhenMailpitConnectionStringPresent()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:mailpit"] = "Endpoint=http://localhost:1025"
        });

        // Act
        builder.AddEmail();

        // Assert
        var settings = builder.Services.BuildServiceProvider()
            .GetRequiredService<IOptions<EmailSettings>>().Value;
        settings.Host.ShouldBe("localhost");
        settings.Port.ShouldBe(1025);
        settings.UserName.ShouldBe(string.Empty);
        settings.AppPassword.ShouldBe(string.Empty);
        settings.TlsMode.ShouldBe(SecureSocketOptions.None);
    }
}