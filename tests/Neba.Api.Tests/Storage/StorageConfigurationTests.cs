using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.BackgroundJobs;
using Neba.Api.Storage;
using Neba.Api.Uploads;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Storage;

[UnitTest]
[Component("Storage")]
public sealed class StorageConfigurationTests
{
    [Fact(DisplayName = "UseUploadCleanupJob should register the orphaned-uploads job as a recurring job every other day at 5 AM")]
    public void UseUploadCleanupJob_ShouldRegisterRecurringJob_EveryOtherDayAt5Am()
    {
        // Arrange
        var schedulerMock = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        schedulerMock
            .Setup(s => s.AddOrUpdateRecurring(
                "cleanup-orphaned-uploads",
                It.IsAny<CleanupOrphanedUploadsJob>(),
                "0 5 */2 * *"))
            .Verifiable();

        var builder = WebApplication.CreateSlimBuilder();
        builder.Services.AddSingleton(schedulerMock.Object);
        var app = builder.Build();

        // Act
        app.UseUploadCleanupJob();

        // Assert
        schedulerMock.VerifyAll();
    }
}
