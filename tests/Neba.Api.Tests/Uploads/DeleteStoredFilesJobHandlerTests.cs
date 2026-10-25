using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

using Neba.Api.Storage;
using Neba.Api.Uploads;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Uploads;

[UnitTest]
[Component("Uploads")]
public sealed class DeleteStoredFilesJobHandlerTests
{
    private readonly Mock<IFileStorageService> _fileStorageService = new(MockBehavior.Strict);
    private readonly FakeLogger<DeleteStoredFilesJobHandler> _logger = new();

    private DeleteStoredFilesJobHandler CreateHandler()
        => new(_fileStorageService.Object, _logger);

    private static DeleteStoredFilesJob CreateJob(params BlobReference[] files)
        => new() { Files = files };

    [Fact(DisplayName = "ExecuteAsync deletes every file in the job")]
    public async Task ExecuteAsync_ShouldDeleteEveryFile()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var first = new BlobReference { Container = "bowlneba-public", Path = "a.png" };
        var second = new BlobReference { Container = "bowlneba-public", Path = "b.png" };
        _fileStorageService.Setup(s => s.DeleteAsync(first.Container, first.Path, ct)).ReturnsAsync(true);
        _fileStorageService.Setup(s => s.DeleteAsync(second.Container, second.Path, ct)).ReturnsAsync(true);

        // Act
        await CreateHandler().ExecuteAsync(CreateJob(first, second), ct);

        // Assert
        _fileStorageService.VerifyAll();
        _logger.Collector.GetSnapshot().Count(record => record.Level == LogLevel.Debug).ShouldBe(2);
    }

    [Fact(DisplayName = "ExecuteAsync logs a warning and keeps deleting when one delete fails")]
    public async Task ExecuteAsync_ShouldLogWarningAndContinue_WhenDeleteFails()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var failing = new BlobReference { Container = "bowlneba-public", Path = "bad.png" };
        var healthy = new BlobReference { Container = "bowlneba-public", Path = "good.png" };
        _fileStorageService.Setup(s => s.DeleteAsync(failing.Container, failing.Path, ct)).ThrowsAsync(new InvalidOperationException("boom"));
        _fileStorageService.Setup(s => s.DeleteAsync(healthy.Container, healthy.Path, ct)).ReturnsAsync(true);

        // Act
        await CreateHandler().ExecuteAsync(CreateJob(failing, healthy), ct);

        // Assert
        _fileStorageService.VerifyAll();
        var warning = _logger.Collector.GetSnapshot().Single(record => record.Level == LogLevel.Warning);
        warning.Message.ShouldContain("bad.png");
        warning.Exception.ShouldBeOfType<InvalidOperationException>();
    }

    [Fact(DisplayName = "JobName reports the number of files")]
    public void JobName_ShouldReportFileCount()
    {
        // Arrange
        var job = CreateJob(
            new BlobReference { Container = "c", Path = "a" },
            new BlobReference { Container = "c", Path = "b" });

        // Act
        var name = job.JobName;

        // Assert
        name.ShouldBe("DeleteStoredFilesJob: 2 file(s)");
    }
}