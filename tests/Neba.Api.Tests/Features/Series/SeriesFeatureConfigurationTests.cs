using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.BackgroundJobs;
using Neba.Api.Features.Series;
using Neba.Api.Features.Series.MakeDueStagesOfficial;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesFeatureConfigurationTests
{
    [Fact(DisplayName = "UseSeriesMakeOfficialJob should schedule the make-official job every 15 minutes")]
    public void UseSeriesMakeOfficialJob_ShouldRegisterRecurringJob_EveryFifteenMinutes()
    {
        // Arrange
        var scheduler = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        scheduler.Setup(s => s.AddOrUpdateRecurring("series-make-official", new MakeDueStagesOfficialJob(), "0 23 * * 1"));
        var builder = WebApplication.CreateSlimBuilder();
        builder.Services.AddSingleton(scheduler.Object);
        using var app = builder.Build();

        // Act
        app.UseSeriesMakeOfficialJob();

        // Assert
        scheduler.Verify(
            s => s.AddOrUpdateRecurring("series-make-official", new MakeDueStagesOfficialJob(), "0 23 * * 1"),
            Times.Once);
        scheduler.Invocations.ShouldHaveSingleItem();
    }
}