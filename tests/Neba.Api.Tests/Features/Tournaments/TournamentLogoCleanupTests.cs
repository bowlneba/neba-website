using Neba.Api.BackgroundJobs;
using Neba.Api.Features.Tournaments;
using Neba.Api.Features.Tournaments.EditTournament;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Storage;

namespace Neba.Api.Tests.Features.Tournaments;

[UnitTest]
[Component("Tournaments")]
public sealed class TournamentLogoCleanupTests
{
    private readonly Mock<IBackgroundJobScheduler> _scheduler = new(MockBehavior.Strict);

    [Fact(DisplayName = "ScheduleDeleteOfReplacedLogo enqueues a delete job for the previous logo when the logo is replaced")]
    public void ScheduleDeleteOfReplacedLogo_ShouldEnqueueDeleteJob_WhenLogoReplaced()
    {
        // Arrange
        var previousLogo = StoredFileFactory.Create(container: "logos", path: "old.png");
        var currentLogo = StoredFileFactory.Create(container: "logos", path: "new.png");
        DeleteTournamentFilesJob? enqueued = null;
        _scheduler
            .Setup(s => s.Enqueue(It.IsAny<DeleteTournamentFilesJob>()))
            .Callback<DeleteTournamentFilesJob>(job => enqueued = job)
            .Returns("job-id");

        // Act
        TournamentLogoCleanup.ScheduleDeleteOfReplacedLogo(_scheduler.Object, previousLogo, currentLogo);

        // Assert
        enqueued.ShouldNotBeNull();
        var file = enqueued.Files.ShouldHaveSingleItem();
        file.Container.ShouldBe("logos");
        file.Path.ShouldBe("old.png");
    }

    [Fact(DisplayName = "ScheduleDeleteOfReplacedLogo enqueues a delete job when the logo is cleared")]
    public void ScheduleDeleteOfReplacedLogo_ShouldEnqueueDeleteJob_WhenLogoCleared()
    {
        // Arrange
        var previousLogo = StoredFileFactory.Create(container: "logos", path: "old.png");
        DeleteTournamentFilesJob? enqueued = null;
        _scheduler
            .Setup(s => s.Enqueue(It.IsAny<DeleteTournamentFilesJob>()))
            .Callback<DeleteTournamentFilesJob>(job => enqueued = job)
            .Returns("job-id");

        // Act
        TournamentLogoCleanup.ScheduleDeleteOfReplacedLogo(_scheduler.Object, previousLogo, null);

        // Assert
        enqueued.ShouldNotBeNull();
        enqueued.Files.ShouldHaveSingleItem().Path.ShouldBe("old.png");
    }

    [Fact(DisplayName = "ScheduleDeleteOfReplacedLogo enqueues a delete job when only the container differs")]
    public void ScheduleDeleteOfReplacedLogo_ShouldEnqueueDeleteJob_WhenOnlyContainerDiffers()
    {
        // Arrange
        var previousLogo = StoredFileFactory.Create(container: "container-a", path: "logo.png");
        var currentLogo = StoredFileFactory.Create(container: "container-b", path: "logo.png");
        DeleteTournamentFilesJob? enqueued = null;
        _scheduler
            .Setup(s => s.Enqueue(It.IsAny<DeleteTournamentFilesJob>()))
            .Callback<DeleteTournamentFilesJob>(job => enqueued = job)
            .Returns("job-id");

        // Act
        TournamentLogoCleanup.ScheduleDeleteOfReplacedLogo(_scheduler.Object, previousLogo, currentLogo);

        // Assert
        enqueued.ShouldNotBeNull();
        enqueued.Files.ShouldHaveSingleItem().Container.ShouldBe("container-a");
    }

    [Fact(DisplayName = "ScheduleDeleteOfReplacedLogo does not enqueue when there was no previous logo")]
    public void ScheduleDeleteOfReplacedLogo_ShouldNotEnqueue_WhenNoPreviousLogo()
    {
        // Arrange
        var currentLogo = StoredFileFactory.Create();

        // Act
        var act = () => TournamentLogoCleanup.ScheduleDeleteOfReplacedLogo(_scheduler.Object, null, currentLogo);

        // Assert — a strict mock with no setup throws if Enqueue is called
        Should.NotThrow(act);
    }

    [Fact(DisplayName = "ScheduleDeleteOfReplacedLogo does not enqueue when the logo file is unchanged")]
    public void ScheduleDeleteOfReplacedLogo_ShouldNotEnqueue_WhenSameFile()
    {
        // Arrange
        var previousLogo = StoredFileFactory.Create(contentType: "image/png", sizeInBytes: 1);
        var currentLogo = StoredFileFactory.Create(contentType: "image/jpeg", sizeInBytes: 2);

        // Act
        var act = () => TournamentLogoCleanup.ScheduleDeleteOfReplacedLogo(_scheduler.Object, previousLogo, currentLogo);

        // Assert — a strict mock with no setup throws if Enqueue is called
        Should.NotThrow(act);
    }
}
