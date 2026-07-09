using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Contracts.Security;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Security;

[UnitTest]
[Component("Security")]
public sealed class PolicyExtensionsTests
{
    [Fact(DisplayName = "AddNebaPolicies should succeed when caller has an article management permission")]
    public async Task AddNebaPolicies_ShouldSucceed_WhenCallerHasArticleManagementPermission()
    {
        // Arrange
        var provider = BuildAuthorizationServiceProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();
        var identity = new ClaimsIdentity([new Claim(Permissions.ClaimType, Permissions.DeleteArticle.Value)]);
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await authorizationService.AuthorizeAsync(principal, Permissions.CanManageArticlesPolicyName);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Fact(DisplayName = "AddNebaPolicies should fail when caller has no article management permission")]
    public async Task AddNebaPolicies_ShouldFail_WhenCallerHasNoArticleManagementPermission()
    {
        // Arrange
        var provider = BuildAuthorizationServiceProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();
        var identity = new ClaimsIdentity([new Claim(Permissions.ClaimType, Permissions.Read.Value)]);
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await authorizationService.AuthorizeAsync(principal, Permissions.CanManageArticlesPolicyName);

        // Assert
        result.Succeeded.ShouldBeFalse();
    }

    private static ServiceProvider BuildAuthorizationServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorizationBuilder().AddNebaPolicies();
        return services.BuildServiceProvider();
    }
}
