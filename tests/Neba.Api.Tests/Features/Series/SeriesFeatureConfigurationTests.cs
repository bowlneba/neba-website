using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.BackgroundJobs;
using Neba.Api.Features.Series;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesFeatureConfigurationTests
{
    [Fact(DisplayName = "UseSeriesRemoveMakeOfficialJob should remove the old make-official recurring job and schedule nothing")]
    public void UseSeriesRemoveMakeOfficialJob_ShouldRemoveRecurringJob()
    {
        // Arrange
        var scheduler = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        scheduler.Setup(s => s.RemoveRecurring("series-make-official"));
        var builder = WebApplication.CreateSlimBuilder();
        builder.Services.AddSingleton(scheduler.Object);
        using var app = builder.Build();

        // Act
        app.UseSeriesRemoveMakeOfficialJob();

        // Assert
        scheduler.Verify(s => s.RemoveRecurring("series-make-official"), Times.Once);
        scheduler.Invocations.ShouldHaveSingleItem();
    }
}