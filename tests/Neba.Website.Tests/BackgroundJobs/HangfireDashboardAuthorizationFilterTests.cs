using Neba.TestFactory.Attributes;

namespace Neba.Website.Tests.BackgroundJobs;

[UnitTest]
[Component("Website.BackgroundJobs")]
public sealed class HangfireDashboardAuthorizationFilterTests
{
    [Fact(DisplayName = "Should always authorize in development")]
    public void Authorize_ShouldReturnTrue()
    {
        // Arrange
        var filter = new Server.BackgroundJobs.HangfireDashboardAuthorizationFilter();
        // DashboardContext cannot be mocked (no parameterless constructor)
        // and is not used by the implementation, so null is acceptable
        Hangfire.Dashboard.DashboardContext context = null!;

        // Act
        var result = filter.Authorize(context);

        // Assert
        result.ShouldBe(true);
    }
}