using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Account;

namespace Neba.Website.Tests.Account;

[UnitTest]
[Component("Website.Account.AccountConfiguration")]
public sealed class AccountConfigurationTests
{
    private static IHostEnvironment CreateEnvironment(string environmentName)
        => new HostingEnvironment { EnvironmentName = environmentName };

    [Fact(DisplayName = "AddAccountServices should configure the auth cookie as HttpOnly with sliding expiration")]
    public void AddAccountServices_ShouldConfigureCookie_AsHttpOnlyWithSlidingExpiration()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        services.AddAccountServices(configuration, CreateEnvironment(Environments.Development));

        // Assert
        var options = services.BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        options.Cookie.Name.ShouldBe(".Neba.Auth");
        options.Cookie.HttpOnly.ShouldBeTrue();
        options.SlidingExpiration.ShouldBeTrue();
        options.ExpireTimeSpan.ShouldBe(TimeSpan.FromDays(7));
        options.LoginPath.Value.ShouldBe("/account/login");
        options.LogoutPath.Value.ShouldBe("/account/logout");
    }

    [Fact(DisplayName = "AddAccountServices should set the cookie domain in Production")]
    public void AddAccountServices_ShouldSetCookieDomain_InProduction()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        services.AddAccountServices(configuration, CreateEnvironment(Environments.Production));

        // Assert
        var options = services.BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        options.Cookie.Domain.ShouldBe(".bowlneba.com");
    }

    [Fact(DisplayName = "AddAccountServices should not set the cookie domain outside Production")]
    public void AddAccountServices_ShouldNotSetCookieDomain_OutsideProduction()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        services.AddAccountServices(configuration, CreateEnvironment(Environments.Development));

        // Assert
        var options = services.BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        options.Cookie.Domain.ShouldBeNull();
    }

    [Fact(DisplayName = "AddAccountServices should register the cascading authentication state supplier")]
    public void AddAccountServices_ShouldRegisterCascadingAuthenticationState()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        services.AddAccountServices(configuration, CreateEnvironment(Environments.Development));

        // Assert (ICascadingValueSupplier is internal to Microsoft.AspNetCore.Components)
        services.ShouldContain(sd => sd.ServiceType.Name == "ICascadingValueSupplier");
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
        services.AddAccountServices(configuration, CreateEnvironment(Environments.Development));

        // Assert
        var settings = services.BuildServiceProvider().GetRequiredService<AdminLoginSettings>();
        settings.Email.ShouldBe("admin@bowlneba.com");
        settings.Password.ShouldBe("super-secret");
    }
}