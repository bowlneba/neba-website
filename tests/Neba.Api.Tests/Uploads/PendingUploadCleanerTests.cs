using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Uploads;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Storage;
using Neba.TestFactory.Uploads;

namespace Neba.Api.Tests.Uploads;

[IntegrationTest]
[Component("Uploads")]
[Collection<AppDbContextFixture>]
public sealed class PendingUploadCleanerTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact(DisplayName = "RemoveClaimedAsync does nothing when file is null")]
    public async Task RemoveClaimedAsync_ShouldDoNothing_WhenFileIsNull()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var pendingUpload = PendingUploadFactory.Create(container: "unrelated-container", path: "unrelated-file.jpg");
        await _dbContext.PendingUploads.AddAsync(pendingUpload, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        await PendingUploadCleaner.RemoveClaimedAsync(_dbContext, file: null, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Assert
        var count = await _dbContext.PendingUploads.CountAsync(ct);
        count.ShouldBe(1);
    }

    [Fact(DisplayName = "RemoveClaimedAsync removes the pending upload matching the file's container and path")]
    public async Task RemoveClaimedAsync_ShouldRemoveMatchingPendingUpload_WhenFileIsClaimed()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var file = StoredFileFactory.Create(container: "claimed-container", path: "claimed-file.jpg");
        var pendingUpload = PendingUploadFactory.Create(container: file.Container, path: file.Path);
        await _dbContext.PendingUploads.AddAsync(pendingUpload, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        await PendingUploadCleaner.RemoveClaimedAsync(_dbContext, file, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Assert
        var stillPending = await _dbContext.PendingUploads.AsNoTracking()
            .AnyAsync(p => p.Container == file.Container && p.Path == file.Path, ct);
        stillPending.ShouldBeFalse();
    }

    [Fact(DisplayName = "RemoveClaimedAsync does not remove pending uploads for a different container or path")]
    public async Task RemoveClaimedAsync_ShouldNotRemoveUnrelatedPendingUploads()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var file = StoredFileFactory.Create(container: "claimed-container", path: "claimed-file.jpg");
        var unrelatedPendingUpload = PendingUploadFactory.Create(container: "unrelated-container", path: "unrelated-file.jpg");
        await _dbContext.PendingUploads.AddAsync(unrelatedPendingUpload, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        await PendingUploadCleaner.RemoveClaimedAsync(_dbContext, file, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Assert
        var stillPending = await _dbContext.PendingUploads.AsNoTracking()
            .AnyAsync(p => p.Container == "unrelated-container" && p.Path == "unrelated-file.jpg", ct);
        stillPending.ShouldBeTrue();
    }

    [Fact(DisplayName = "RemoveClaimedAsync does nothing when no pending upload matches the file")]
    public async Task RemoveClaimedAsync_ShouldDoNothing_WhenNoPendingUploadMatchesFile()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var file = StoredFileFactory.Create(container: "no-match-container", path: "no-match-file.jpg");

        // Act
        await PendingUploadCleaner.RemoveClaimedAsync(_dbContext, file, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Assert
        var count = await _dbContext.PendingUploads.CountAsync(ct);
        count.ShouldBe(0);
    }
}