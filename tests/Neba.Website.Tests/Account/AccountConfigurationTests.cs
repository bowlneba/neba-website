using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Account;

namespace Neba.Website.Tests.Account;

[UnitTest]
[Component("Website.Account.AccountConfiguration")]
public sealed class AccountConfigurationTests
{
    [Fact(DisplayName = "AddAccountServices should configure the auth cookie as HttpOnly with sliding expiration")]
    public void AddAccountServices_ShouldConfigureCookie_AsHttpOnlyWithSlidingExpiration()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        services.AddAccountServices(configuration);

        // Assert
        var options = services.BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        options.Cookie.HttpOnly.ShouldBeTrue();
        options.SlidingExpiration.ShouldBeTrue();
        options.ExpireTimeSpan.ShouldBe(TimeSpan.FromDays(7));
        options.LoginPath.Value.ShouldBe("/account/login");
        options.LogoutPath.Value.ShouldBe("/account/logout");
    }

    [Fact(DisplayName = "AddAccountServices should register the cascading authentication state provider")]
    public void AddAccountServices_ShouldRegisterCascadingAuthenticationState()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        services.AddAccountServices(configuration);

        // Assert
        services.ShouldContain(sd => sd.ServiceType == typeof(AuthenticationStateProvider));
    }

    [Fact(DisplayName = "AddAccountServices should bind AdminLoginSettings from the Admin configuration section")]
    public void AddAccountServices_ShouldBindAdminLoginSettings()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:Email"] = "admin@bowlneba.com",
                ["Admin:Password"] = "super-secret",
            })
            .Build();

        // Act
        services.AddAccountServices(configuration);

        // Assert
        var settings = services.BuildServiceProvider().GetRequiredService<AdminLoginSettings>();
        settings.Email.ShouldBe("admin@bowlneba.com");
        settings.Password.ShouldBe("super-secret");
    }
}
