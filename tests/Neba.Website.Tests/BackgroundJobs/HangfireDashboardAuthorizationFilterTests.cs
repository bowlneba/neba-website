using Neba.TestFactory.Attributes;

namespace Neba.Website.Tests.BackgroundJobs;

[UnitTest]
[Component("Website.BackgroundJobs")]
public sealed class HangfireDashboardAuthorizationFilterTests
{
    [Fact]
    public void Authorize_ShouldReturnTrue()
    {
        // Arrange
        var filter = new Server.BackgroundJobs.HangfireDashboardAuthorizationFilter();
        var context = new Mock<Hangfire.Dashboard.DashboardContext>(MockBehavior.Strict);

        // Act
        var result = filter.Authorize(context.Object);

        // Assert
        result.ShouldBe(true);
    }
}