using Neba.Api.BackgroundJobs;
using Neba.Api.Uploads;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Storage;

namespace Neba.Api.Tests.Uploads;

[UnitTest]
[Component("Uploads")]
public sealed class StoredFileCleanupTests
{
    private readonly Mock<IBackgroundJobScheduler> _scheduler = new(MockBehavior.Strict);

    [Fact(DisplayName = "ScheduleDeleteOfReplaced enqueues a delete job for the previous file when the file is replaced")]
    public void ScheduleDeleteOfReplaced_ShouldEnqueueDeleteJob_WhenFileReplaced()
    {
        // Arrange
        var previousFile = StoredFileFactory.Create(container: "files", path: "old.png");
        var currentFile = StoredFileFactory.Create(container: "files", path: "new.png");
        DeleteStoredFilesJob? enqueued = null;
        _scheduler
            .Setup(s => s.Enqueue(It.IsAny<DeleteStoredFilesJob>()))
            .Callback<DeleteStoredFilesJob>(job => enqueued = job)
            .Returns("job-id");

        // Act
        StoredFileCleanup.ScheduleDeleteOfReplaced(_scheduler.Object, previousFile, currentFile);

        // Assert
        enqueued.ShouldNotBeNull();
        var file = enqueued.Files.ShouldHaveSingleItem();
        file.Container.ShouldBe("files");
        file.Path.ShouldBe("old.png");
    }

    [Fact(DisplayName = "ScheduleDeleteOfReplaced enqueues a delete job when the file is cleared")]
    public void ScheduleDeleteOfReplaced_ShouldEnqueueDeleteJob_WhenFileCleared()
    {
        // Arrange
        var previousFile = StoredFileFactory.Create(container: "files", path: "old.png");
        DeleteStoredFilesJob? enqueued = null;
        _scheduler
            .Setup(s => s.Enqueue(It.IsAny<DeleteStoredFilesJob>()))
            .Callback<DeleteStoredFilesJob>(job => enqueued = job)
            .Returns("job-id");

        // Act
        StoredFileCleanup.ScheduleDeleteOfReplaced(_scheduler.Object, previousFile, null);

        // Assert
        enqueued.ShouldNotBeNull();
        enqueued.Files.ShouldHaveSingleItem().Path.ShouldBe("old.png");
    }

    [Fact(DisplayName = "ScheduleDeleteOfReplaced enqueues a delete job when only the container differs")]
    public void ScheduleDeleteOfReplaced_ShouldEnqueueDeleteJob_WhenOnlyContainerDiffers()
    {
        // Arrange
        var previousFile = StoredFileFactory.Create(container: "container-a", path: "file.png");
        var currentFile = StoredFileFactory.Create(container: "container-b", path: "file.png");
        DeleteStoredFilesJob? enqueued = null;
        _scheduler
            .Setup(s => s.Enqueue(It.IsAny<DeleteStoredFilesJob>()))
            .Callback<DeleteStoredFilesJob>(job => enqueued = job)
            .Returns("job-id");

        // Act
        StoredFileCleanup.ScheduleDeleteOfReplaced(_scheduler.Object, previousFile, currentFile);

        // Assert
        enqueued.ShouldNotBeNull();
        enqueued.Files.ShouldHaveSingleItem().Container.ShouldBe("container-a");
    }

    [Fact(DisplayName = "ScheduleDeleteOfReplaced does not enqueue when there was no previous file")]
    public void ScheduleDeleteOfReplaced_ShouldNotEnqueue_WhenNoPreviousFile()
    {
        // Arrange
        var currentFile = StoredFileFactory.Create();

        // Act
        void Act() => StoredFileCleanup.ScheduleDeleteOfReplaced(_scheduler.Object, null, currentFile);

        // Assert — a strict mock with no setup throws if Enqueue is called
        Should.NotThrow(Act);
    }

    [Fact(DisplayName = "ScheduleDeleteOfReplaced does not enqueue when the file is unchanged")]
    public void ScheduleDeleteOfReplaced_ShouldNotEnqueue_WhenSameFile()
    {
        // Arrange
        var previousFile = StoredFileFactory.Create(contentType: "image/png", sizeInBytes: 1);
        var currentFile = StoredFileFactory.Create(contentType: "image/jpeg", sizeInBytes: 2);

        // Act
        void Act() => StoredFileCleanup.ScheduleDeleteOfReplaced(_scheduler.Object, previousFile, currentFile);

        // Assert — a strict mock with no setup throws if Enqueue is called
        Should.NotThrow(Act);
    }
}