using Neba.TestFactory.Attributes;

namespace Neba.Infrastructure.Tests.BackgroundJobs;

[UnitTest]
[Component("Infrastructure.BackgroundJobs")]
public sealed class HangfireApiDashboardAuthorizationFilterTests
{
    [Fact(DisplayName = "Should always authorize in development")]
    public void Authorize_ShouldReturnTrue()
    {
#nullable disable
        // Arrange
        var filter = new Infrastructure.BackgroundJobs.HangfireApiDashboardAuthorizationFilter();
        // DashboardContext cannot be mocked (no parameterless constructor)
        // and is not used by the implementation, so null is acceptable
        Hangfire.Dashboard.DashboardContext context = null;

        // Act
        var result = filter.Authorize(context);

        // Assert
        result.ShouldBe(true);
#nullable enable
    }
}