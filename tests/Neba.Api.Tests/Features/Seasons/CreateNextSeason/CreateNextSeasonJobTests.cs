using Neba.Api.Features.Seasons.CreateNextSeason;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Seasons.CreateNextSeason;

[UnitTest]
[Component("Seasons")]
public sealed class CreateNextSeasonJobTests
{
    [Fact(DisplayName = "JobName should be a fixed, descriptive name")]
    public void JobName_ShouldBeFixedDescriptiveName()
    {
        // Arrange
        var job = new CreateNextSeasonJob();

        // Act & Assert
        job.JobName.ShouldBe("Create Next Season Job");
    }
}
