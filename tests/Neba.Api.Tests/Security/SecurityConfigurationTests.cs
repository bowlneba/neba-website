using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Neba.Api.Contracts.Security.Authorization;
using Neba.Api.Database;
using Neba.Api.Security;
using Neba.Api.Security.Domain;
using Neba.TestFactory.Attributes;

using Npgsql;

namespace Neba.Api.Tests.Security;

[IntegrationTest]
[Component("Security")]
public sealed class SecurityConfigurationTests
{
    private static WebApplicationBuilder CreateBuilderWithValidJwtSettings()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JwtSettings:Issuer"] = "https://bowlneba.com",
            ["JwtSettings:Audience"] = "https://bowlneba.com",
            ["JwtSettings:SigningKey"] = new string('a', 32),
        });

        return builder;
    }

    [Fact(DisplayName = "UseSecurityInfrastructure should return the same WebApplication instance")]
    public void UseSecurityInfrastructure_ShouldReturnSameApp()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization();
        var app = builder.Build();

        // Act
        var result = app.UseSecurityInfrastructure();

        // Assert
        result.ShouldBeSameAs(app);
    }

    [Fact(DisplayName = "AddSecurity should register SecurityDbContext")]
    public void AddSecurity_ShouldRegisterSecurityDbContext()
    {
        // Arrange
        var builder = CreateBuilderWithValidJwtSettings();

        // Act
        builder.AddSecurity();

        // Assert
        builder.Services.ShouldContain(sd => sd.ServiceType == typeof(SecurityDbContext));
    }

    [Fact(DisplayName = "AddSecurity should register the authorization policy and permission-based authorization services")]
    public void AddSecurity_ShouldRegisterAuthorizationServices()
    {
        // Arrange
        var builder = CreateBuilderWithValidJwtSettings();

        // Act
        builder.AddSecurity();

        // Assert
        builder.Services.ShouldContain(sd => sd.ServiceType == typeof(IAuthorizationPolicyProvider)
            && sd.ImplementationType == typeof(PermissionPolicyProvider));
        builder.Services.ShouldContain(sd => sd.ServiceType == typeof(IAuthorizationHandler)
            && sd.ImplementationType == typeof(PermissionAuthorizationHandler));
    }

    [Fact(DisplayName = "AddSecurity should register JwtSettings and IJwtTokenService")]
    public void AddSecurity_ShouldRegisterJwtSettingsAndTokenService()
    {
        // Arrange
        var builder = CreateBuilderWithValidJwtSettings();

        // Act
        builder.AddSecurity();

        // Assert
        builder.Services.ShouldContain(sd => sd.ServiceType == typeof(IJwtTokenService)
            && sd.ImplementationType == typeof(JwtTokenService));
    }

    [Fact(DisplayName = "AddSecurity should return the same WebApplicationBuilder instance")]
    public void AddSecurity_ShouldReturnSameBuilder()
    {
        // Arrange
        var builder = CreateBuilderWithValidJwtSettings();

        // Act
        var result = builder.AddSecurity();

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact(DisplayName = "AddSecurity should configure identity password and lockout options")]
    public void AddSecurity_ShouldConfigureIdentityOptions()
    {
        // Arrange
        var builder = CreateBuilderWithValidJwtSettings();
        builder.Services.AddSingleton(new NpgsqlDataSourceBuilder("Host=localhost;Database=x;Username=y;Password=z").Build());
        builder.AddSecurity();
        var provider = builder.Services.BuildServiceProvider();

        // Act
        var identityOptions = provider.GetRequiredService<IOptions<IdentityOptions>>().Value;

        // Assert
        identityOptions.Password.RequireDigit.ShouldBeTrue();
        identityOptions.Password.RequiredLength.ShouldBe(8);
        identityOptions.Password.RequireNonAlphanumeric.ShouldBeFalse();
        identityOptions.SignIn.RequireConfirmedEmail.ShouldBeTrue();
        identityOptions.User.RequireUniqueEmail.ShouldBeTrue();
        identityOptions.Lockout.AllowedForNewUsers.ShouldBeTrue();
        identityOptions.Lockout.MaxFailedAccessAttempts.ShouldBe(5);
        identityOptions.Lockout.DefaultLockoutTimeSpan.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact(DisplayName = "AddSecurity should configure JWT bearer as the default authentication and challenge scheme with matching token validation parameters")]
    public void AddSecurity_ShouldConfigureJwtBearerAuthentication()
    {
        // Arrange
        var builder = CreateBuilderWithValidJwtSettings();
        builder.Services.AddSingleton(new NpgsqlDataSourceBuilder("Host=localhost;Database=x;Username=y;Password=z").Build());
        builder.AddSecurity();
        var provider = builder.Services.BuildServiceProvider();

        // Act
        var authenticationOptions = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
        var jwtBearerOptions = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        // Assert
        authenticationOptions.DefaultAuthenticateScheme.ShouldBe(JwtBearerDefaults.AuthenticationScheme);
        authenticationOptions.DefaultChallengeScheme.ShouldBe(JwtBearerDefaults.AuthenticationScheme);
        jwtBearerOptions.TokenValidationParameters.ValidIssuer.ShouldBe("https://bowlneba.com");
        jwtBearerOptions.TokenValidationParameters.ValidAudience.ShouldBe("https://bowlneba.com");
        jwtBearerOptions.TokenValidationParameters.ValidateIssuerSigningKey.ShouldBeTrue();
    }

    [Fact(DisplayName = "AddSecurity should register the Authenticated authorization policy requiring an authenticated user")]
    public async Task AddSecurity_ShouldRegisterAuthenticatedPolicy()
    {
        // Arrange
        var builder = CreateBuilderWithValidJwtSettings();
        builder.Services.AddSingleton(new NpgsqlDataSourceBuilder("Host=localhost;Database=x;Username=y;Password=z").Build());
        builder.AddSecurity();
        var provider = builder.Services.BuildServiceProvider();
        var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();

        // Act
        var policy = await policyProvider.GetPolicyAsync(SecurityConfiguration.AuthenticatedPolicy);

        // Assert
        policy.ShouldNotBeNull();
        policy.Requirements.ShouldContain(r => r is DenyAnonymousAuthorizationRequirement);
    }

    [Fact(DisplayName = "AddSecurity should configure SecurityDbContext to use the registered NpgsqlDataSource")]
    public void AddSecurity_ShouldConfigureSecurityDbContextOptions()
    {
        // Arrange
        var builder = CreateBuilderWithValidJwtSettings();
        builder.Services.AddSingleton(new NpgsqlDataSourceBuilder("Host=localhost;Database=x;Username=y;Password=z").Build());
        builder.AddSecurity();
        var provider = builder.Services.BuildServiceProvider();

        // Act
        var options = provider.GetRequiredService<DbContextOptions<SecurityDbContext>>();

        // Assert
        options.Extensions.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "AddSecurity should throw when the JwtSettings configuration section is missing")]
    public void AddSecurity_ShouldThrow_WhenJwtSettingsSectionIsMissing()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();

        // Act
        WebApplicationBuilder Act() => builder.AddSecurity();

        // Assert
        Should.Throw<InvalidOperationException>((Func<WebApplicationBuilder>)Act)
            .Message.ShouldContain("JwtSettings");
    }

    [Fact(DisplayName = "AddSecurity should throw when JwtSettings SigningKey is empty")]
    public void AddSecurity_ShouldThrow_WhenSigningKeyIsEmpty()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JwtSettings:Issuer"] = "https://bowlneba.com",
            ["JwtSettings:Audience"] = "https://bowlneba.com",
            ["JwtSettings:SigningKey"] = string.Empty,
        });

        // Act
        WebApplicationBuilder Act() => builder.AddSecurity();

        // Assert
        Should.Throw<InvalidOperationException>((Func<WebApplicationBuilder>)Act)
            .Message.ShouldContain("SigningKey must not be empty");
    }

    [Fact(DisplayName = "AddSecurity should throw when JwtSettings SigningKey is shorter than 32 bytes")]
    public void AddSecurity_ShouldThrow_WhenSigningKeyIsTooShort()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JwtSettings:Issuer"] = "https://bowlneba.com",
            ["JwtSettings:Audience"] = "https://bowlneba.com",
            ["JwtSettings:SigningKey"] = new string('a', 16),
        });

        // Act
        WebApplicationBuilder Act() => builder.AddSecurity();

        // Assert
        Should.Throw<InvalidOperationException>((Func<WebApplicationBuilder>)Act)
            .Message.ShouldContain("at least 32 bytes");
    }
}