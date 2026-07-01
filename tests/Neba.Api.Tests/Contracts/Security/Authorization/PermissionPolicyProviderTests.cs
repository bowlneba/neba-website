using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

using Neba.Api.Contracts.Security.Authorization;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Contracts.Security.Authorization;

[UnitTest]
[Component("Api.Contracts")]
public sealed class PermissionPolicyProviderTests
{
    private static PermissionPolicyProvider CreateProvider()
        => new(Options.Create(new AuthorizationOptions()));

    [Fact(DisplayName = "GetPolicyAsync should return a policy requiring the permission when the policy name has the Permission prefix")]
    public async Task GetPolicyAsync_ShouldReturnPermissionPolicy_WhenPolicyNameHasPermissionPrefix()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var policy = await provider.GetPolicyAsync("Permission:Read");

        // Assert
        policy.ShouldNotBeNull();
        var requirement = policy.Requirements.OfType<PermissionRequirement>().SingleOrDefault();
        requirement.ShouldNotBeNull();
        requirement.Permission.ShouldBe("Read");
    }

    [Fact(DisplayName = "GetPolicyAsync should delegate to the fallback provider when the policy name does not have the Permission prefix")]
    public async Task GetPolicyAsync_ShouldDelegateToFallback_WhenPolicyNameDoesNotHavePermissionPrefix()
    {
        // Arrange
        var authorizationOptions = new AuthorizationOptions();
        authorizationOptions.AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());
        var provider = new PermissionPolicyProvider(Options.Create(authorizationOptions));

        // Act
        var policy = await provider.GetPolicyAsync("Authenticated");

        // Assert
        policy.ShouldNotBeNull();
        policy.Requirements.OfType<PermissionRequirement>().ShouldBeEmpty();
    }

    [Fact(DisplayName = "GetDefaultPolicyAsync should delegate to the fallback provider")]
    public async Task GetDefaultPolicyAsync_ShouldDelegateToFallback()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var policy = await provider.GetDefaultPolicyAsync();

        // Assert
        policy.ShouldNotBeNull();
    }

    [Fact(DisplayName = "GetFallbackPolicyAsync should delegate to the fallback provider")]
    public async Task GetFallbackPolicyAsync_ShouldDelegateToFallback()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var policy = await provider.GetFallbackPolicyAsync();

        // Assert
        policy.ShouldBeNull();
    }
}
