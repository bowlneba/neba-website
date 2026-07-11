using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

using Neba.Api.Database;
using Neba.Api.Storage;
using Neba.Api.Uploads;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Uploads;

namespace Neba.Api.Tests.Uploads;

[IntegrationTest]
[Component("Uploads")]
[Collection<AppDbContextFixture>]
public sealed class CleanupOrphanedUploadsJobHandlerTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly AppDbContext _dbContext = fixture.CreateDbContext();
    private readonly Mock<IFileStorageService> _fileStorageServiceMock = new(MockBehavior.Strict);
    private readonly FakeLogger<CleanupOrphanedUploadsJobHandler> _logger = new();
    private readonly FakeTimeProvider _timeProvider = new(Now);

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    private CleanupOrphanedUploadsJobHandler CreateHandler()
        => new(_dbContext, _fileStorageServiceMock.Object, _timeProvider, _logger);

    [Fact(DisplayName = "Should delete the blob and remove the pending upload row when the upload is older than the orphan threshold")]
    public async Task ExecuteAsync_ShouldDeleteBlobAndRemoveRow_WhenUploadIsOrphaned()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var orphan = PendingUploadFactory.Create(container: "container-orphan-deleted", path: "orphan-deleted.txt", uploadedAtUtc: Now - TimeSpan.FromHours(25));
        await _dbContext.PendingUploads.AddAsync(orphan, ct);
        await _dbContext.SaveChangesAsync(ct);

        _fileStorageServiceMock
            .Setup(s => s.DeleteAsync(orphan.Container, orphan.Path, ct))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        // Act
        await handler.ExecuteAsync(new CleanupOrphanedUploadsJob(), ct);

        // Assert
        _fileStorageServiceMock.VerifyAll();

        var stillExists = await _dbContext.PendingUploads.AsNoTracking()
            .AnyAsync(upload => upload.Container == orphan.Container && upload.Path == orphan.Path, ct);
        stillExists.ShouldBeFalse();
    }

    [Fact(DisplayName = "Should not delete or remove an upload that is within the orphan threshold")]
    public async Task ExecuteAsync_ShouldNotDeleteOrRemove_WhenUploadIsNotOrphaned()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var recent = PendingUploadFactory.Create(container: "container-not-orphaned", path: "not-orphaned.txt", uploadedAtUtc: Now - TimeSpan.FromHours(1));
        await _dbContext.PendingUploads.AddAsync(recent, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();

        // Act
        await handler.ExecuteAsync(new CleanupOrphanedUploadsJob(), ct);

        // Assert — a strict mock with no DeleteAsync setup would throw if called
        _fileStorageServiceMock.VerifyAll();

        var stillExists = await _dbContext.PendingUploads.AsNoTracking()
            .AnyAsync(upload => upload.Container == recent.Container && upload.Path == recent.Path, ct);
        stillExists.ShouldBeTrue();
    }

    [Fact(DisplayName = "Should log a warning and keep the pending upload row when blob deletion fails")]
    public async Task ExecuteAsync_ShouldLogWarningAndKeepRow_WhenBlobDeletionFails()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var orphan = PendingUploadFactory.Create(container: "container-delete-fails", path: "delete-fails.txt", uploadedAtUtc: Now - TimeSpan.FromHours(25));
        await _dbContext.PendingUploads.AddAsync(orphan, ct);
        await _dbContext.SaveChangesAsync(ct);

        _fileStorageServiceMock
            .Setup(s => s.DeleteAsync(orphan.Container, orphan.Path, ct))
            .ReturnsAsync(false);

        var handler = CreateHandler();

        // Act
        await handler.ExecuteAsync(new CleanupOrphanedUploadsJob(), ct);

        // Assert
        _fileStorageServiceMock.VerifyAll();

        var logs = _logger.Collector.GetSnapshot();
        logs.ShouldContain(l =>
            l.Level == LogLevel.Warning &&
            l.Message.Contains(orphan.Container) &&
            l.Message.Contains(orphan.Path));

        var stillExists = await _dbContext.PendingUploads.AsNoTracking()
            .AnyAsync(upload => upload.Container == orphan.Container && upload.Path == orphan.Path, ct);
        stillExists.ShouldBeTrue();
    }

    [Fact(DisplayName = "Should not call the file storage service when there are no pending uploads")]
    public async Task ExecuteAsync_ShouldNotCallFileStorageService_WhenNoPendingUploads()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();

        // Act
        await Should.NotThrowAsync(
            () => handler.ExecuteAsync(new CleanupOrphanedUploadsJob(), ct));

        // Assert
        _fileStorageServiceMock.VerifyAll();
    }

    [Fact(DisplayName = "Should process multiple orphaned uploads and leave non-orphaned uploads untouched")]
    public async Task ExecuteAsync_ShouldProcessMultipleOrphans_AndLeaveRecentUploadsUntouched()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var orphan1 = PendingUploadFactory.Create(container: "container-a", path: "path-a", uploadedAtUtc: Now - TimeSpan.FromHours(48));
        var orphan2 = PendingUploadFactory.Create(container: "container-b", path: "path-b", uploadedAtUtc: Now - TimeSpan.FromHours(30));
        var recent = PendingUploadFactory.Create(container: "container-c", path: "path-c", uploadedAtUtc: Now - TimeSpan.FromMinutes(30));

        await _dbContext.PendingUploads.AddRangeAsync([orphan1, orphan2, recent], ct);
        await _dbContext.SaveChangesAsync(ct);

        _fileStorageServiceMock
            .Setup(s => s.DeleteAsync(orphan1.Container, orphan1.Path, ct))
            .ReturnsAsync(true);
        _fileStorageServiceMock
            .Setup(s => s.DeleteAsync(orphan2.Container, orphan2.Path, ct))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        // Act
        await handler.ExecuteAsync(new CleanupOrphanedUploadsJob(), ct);

        // Assert
        _fileStorageServiceMock.VerifyAll();

        var remainingPaths = await _dbContext.PendingUploads.AsNoTracking()
            .Select(upload => upload.Path)
            .ToListAsync(ct);
        remainingPaths.ShouldBe([recent.Path]);
    }
}
