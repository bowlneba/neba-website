using Neba.Api.Uploads;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Uploads;

[UnitTest]
[Component("Uploads")]
public sealed class CleanupOrphanedUploadsJobTests
{
    [Fact(DisplayName = "JobName should be a fixed, descriptive name")]
    public void JobName_ShouldBeFixedDescriptiveName()
    {
        // Arrange
        var job = new CleanupOrphanedUploadsJob();

        // Act & Assert
        job.JobName.ShouldBe("Cleanup Orphaned Uploads Job");
    }
}
