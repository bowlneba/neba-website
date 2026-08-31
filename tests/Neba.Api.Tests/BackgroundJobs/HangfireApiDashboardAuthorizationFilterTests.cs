using System.Security.Claims;

using Neba.Api.BackgroundJobs;
using Neba.Api.Contracts.Security;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.BackgroundJobs;

[UnitTest]
[Component("Infrastructure.BackgroundJobs")]
public sealed class HangfireApiDashboardAuthorizationFilterTests
{
    [Fact(DisplayName = "Authorize should return true when the user has the ViewBackgroundJobsDashboard permission")]
    public void Authorize_ShouldReturnTrue_WhenUserHasPermission()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(Permissions.ClaimType, Permissions.ViewBackgroundJobsDashboard.Value)],
            authenticationType: "TestAuth"));

        // Act
        var result = HangfireApiDashboardAuthorizationFilter.Authorize(user);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "Authorize should return false when the user lacks the ViewBackgroundJobsDashboard permission")]
    public void Authorize_ShouldReturnFalse_WhenUserLacksPermission()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(Permissions.ClaimType, Permissions.CreateArticle.Value)],
            authenticationType: "TestAuth"));

        // Act
        var result = HangfireApiDashboardAuthorizationFilter.Authorize(user);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "Authorize should return false when the user is unauthenticated")]
    public void Authorize_ShouldReturnFalse_WhenUserIsUnauthenticated()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var result = HangfireApiDashboardAuthorizationFilter.Authorize(user);

        // Assert
        result.ShouldBeFalse();
    }
}