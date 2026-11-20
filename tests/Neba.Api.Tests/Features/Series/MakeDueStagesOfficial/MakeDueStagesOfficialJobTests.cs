using Neba.Api.Features.Series.MakeDueStagesOfficial;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series.MakeDueStagesOfficial;

[UnitTest]
[Component("Series")]
public sealed class MakeDueStagesOfficialJobTests
{
    [Fact(DisplayName = "JobName should be Make Due Series Stages Official")]
    public void JobName_ShouldBeMakeDueSeriesStagesOfficial()
    {
        // Arrange
        var job = new MakeDueStagesOfficialJob();

        // Act
        var name = job.JobName;

        // Assert
        name.ShouldBe("Make Due Series Stages Official");
    }
}