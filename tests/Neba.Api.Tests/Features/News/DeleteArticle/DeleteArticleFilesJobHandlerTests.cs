using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

using Neba.Api.Features.News.DeleteArticle;
using Neba.Api.Storage;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.News.DeleteArticle;

[UnitTest]
[Component("News")]
public sealed class DeleteArticleFilesJobHandlerTests
{
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly FakeLogger<DeleteArticleFilesJobHandler> _logger;

    private readonly DeleteArticleFilesJobHandler _handler;

    public DeleteArticleFilesJobHandlerTests()
    {
        _fileStorageServiceMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        _logger = new FakeLogger<DeleteArticleFilesJobHandler>();

        _handler = new DeleteArticleFilesJobHandler(_fileStorageServiceMock.Object, _logger);
    }

    [Fact(DisplayName = "Should delete every file and log a debug message per file when all deletions succeed")]
    public async Task ExecuteAsync_ShouldDeleteAllFilesAndLogDebug_WhenAllDeletionsSucceed()
    {
        // Arrange
        var file1 = new StoredFileReference { Container = "bowlneba-private", Path = "articles/1/cover.png" };
        var file2 = new StoredFileReference { Container = "bowlneba-private", Path = "articles/1/body.html" };
        var job = new DeleteArticleFilesJob { Files = [file1, file2] };

        _fileStorageServiceMock
            .Setup(s => s.DeleteAsync(file1.Container, file1.Path, TestContext.Current.CancellationToken))
            .Returns(Task.CompletedTask);

        _fileStorageServiceMock
            .Setup(s => s.DeleteAsync(file2.Container, file2.Path, TestContext.Current.CancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        await Should.NotThrowAsync(
            () => _handler.ExecuteAsync(job, TestContext.Current.CancellationToken));

        // Assert
        _fileStorageServiceMock.VerifyAll();

        var logs = _logger.Collector.GetSnapshot();
        logs.ShouldContain(l => l.Level == LogLevel.Debug && l.Message.Contains(file1.Container) && l.Message.Contains(file1.Path));
        logs.ShouldContain(l => l.Level == LogLevel.Debug && l.Message.Contains(file2.Container) && l.Message.Contains(file2.Path));
        logs.ShouldNotContain(l => l.Level >= LogLevel.Warning);
    }

    [Fact(DisplayName = "Should log a warning with the exception and continue processing when a deletion fails")]
    public async Task ExecuteAsync_ShouldLogWarningAndContinue_WhenDeletionFails()
    {
        // Arrange
        var failingFile = new StoredFileReference { Container = "bowlneba-private", Path = "articles/1/cover.png" };
        var succeedingFile = new StoredFileReference { Container = "bowlneba-private", Path = "articles/1/body.html" };
        var job = new DeleteArticleFilesJob { Files = [failingFile, succeedingFile] };
        var thrownException = new InvalidOperationException("Storage unavailable");

        _fileStorageServiceMock
            .Setup(s => s.DeleteAsync(failingFile.Container, failingFile.Path, TestContext.Current.CancellationToken))
            .ThrowsAsync(thrownException);

        _fileStorageServiceMock
            .Setup(s => s.DeleteAsync(succeedingFile.Container, succeedingFile.Path, TestContext.Current.CancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        await Should.NotThrowAsync(
            () => _handler.ExecuteAsync(job, TestContext.Current.CancellationToken));

        // Assert
        _fileStorageServiceMock.VerifyAll();

        var logs = _logger.Collector.GetSnapshot();
        logs.ShouldContain(l =>
            l.Level == LogLevel.Warning &&
            l.Message.Contains(failingFile.Container) &&
            l.Message.Contains(failingFile.Path) &&
            l.Exception == thrownException);

        logs.ShouldContain(l =>
            l.Level == LogLevel.Debug &&
            l.Message.Contains(succeedingFile.Container) &&
            l.Message.Contains(succeedingFile.Path));
    }

    [Fact(DisplayName = "Should not call the file storage service when there are no files")]
    public async Task ExecuteAsync_ShouldNotCallFileStorageService_WhenNoFiles()
    {
        // Arrange
        var job = new DeleteArticleFilesJob { Files = [] };

        // Act
        await Should.NotThrowAsync(
            () => _handler.ExecuteAsync(job, TestContext.Current.CancellationToken));

        // Assert
        _fileStorageServiceMock.VerifyAll();
        _logger.Collector.GetSnapshot().ShouldBeEmpty();
    }
}