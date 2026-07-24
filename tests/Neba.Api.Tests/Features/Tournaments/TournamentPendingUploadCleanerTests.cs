using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Tournaments;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Storage;
using Neba.TestFactory.Uploads;

namespace Neba.Api.Tests.Features.Tournaments;

[IntegrationTest]
[Component("Tournaments")]
[Collection<AppDbContextFixture>]
public sealed class TournamentPendingUploadCleanerTests(AppDbContextFixture fixture)
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

    [Fact(DisplayName = "RemoveClaimedAsync does nothing when logo is null")]
    public async Task RemoveClaimedAsync_ShouldDoNothing_WhenLogoIsNull()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var pendingUpload = PendingUploadFactory.Create(container: "unrelated-container", path: "unrelated-file.jpg");
        await _dbContext.PendingUploads.AddAsync(pendingUpload, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        await TournamentPendingUploadCleaner.RemoveClaimedAsync(_dbContext, logo: null, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Assert
        var count = await _dbContext.PendingUploads.CountAsync(ct);
        count.ShouldBe(1);
    }

    [Fact(DisplayName = "RemoveClaimedAsync removes the pending upload matching the logo's container and path")]
    public async Task RemoveClaimedAsync_ShouldRemoveMatchingPendingUpload_WhenLogoIsClaimed()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var logo = StoredFileFactory.Create(container: "claimed-container", path: "claimed-file.jpg");
        var pendingUpload = PendingUploadFactory.Create(container: logo.Container, path: logo.Path);
        await _dbContext.PendingUploads.AddAsync(pendingUpload, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        await TournamentPendingUploadCleaner.RemoveClaimedAsync(_dbContext, logo, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Assert
        var stillPending = await _dbContext.PendingUploads.AsNoTracking()
            .AnyAsync(p => p.Container == logo.Container && p.Path == logo.Path, ct);
        stillPending.ShouldBeFalse();
    }

    [Fact(DisplayName = "RemoveClaimedAsync does not remove pending uploads for a different container or path")]
    public async Task RemoveClaimedAsync_ShouldNotRemoveUnrelatedPendingUploads()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var logo = StoredFileFactory.Create(container: "claimed-container", path: "claimed-file.jpg");
        var unrelatedPendingUpload = PendingUploadFactory.Create(container: "unrelated-container", path: "unrelated-file.jpg");
        await _dbContext.PendingUploads.AddAsync(unrelatedPendingUpload, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        await TournamentPendingUploadCleaner.RemoveClaimedAsync(_dbContext, logo, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Assert
        var stillPending = await _dbContext.PendingUploads.AsNoTracking()
            .AnyAsync(p => p.Container == "unrelated-container" && p.Path == "unrelated-file.jpg", ct);
        stillPending.ShouldBeTrue();
    }

    [Fact(DisplayName = "RemoveClaimedAsync does nothing when no pending upload matches the logo")]
    public async Task RemoveClaimedAsync_ShouldDoNothing_WhenNoPendingUploadMatchesLogo()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var logo = StoredFileFactory.Create(container: "no-match-container", path: "no-match-file.jpg");

        // Act
        await TournamentPendingUploadCleaner.RemoveClaimedAsync(_dbContext, logo, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Assert
        var count = await _dbContext.PendingUploads.CountAsync(ct);
        count.ShouldBe(0);
    }
}