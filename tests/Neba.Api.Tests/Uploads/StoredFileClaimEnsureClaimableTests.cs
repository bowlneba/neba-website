using ErrorOr;

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
public sealed class StoredFileClaimEnsureClaimableTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private static readonly Error NotUploaded = Error.Validation("Test.NotUploaded", "File was not uploaded.");

    private readonly AppDbContext _dbContext = fixture.CreateDbContext();

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact(DisplayName = "EnsureClaimableAsync succeeds when the file is null")]
    public async Task EnsureClaimableAsync_ShouldSucceed_WhenFileIsNull()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        // Act
        var result = await StoredFileClaim.EnsureClaimableAsync(_dbContext, null, StoredFileFactory.Create(), NotUploaded, ct);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "EnsureClaimableAsync succeeds when the file is the entity's current file")]
    public async Task EnsureClaimableAsync_ShouldSucceed_WhenFileIsCurrentFile()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var currentFile = StoredFileFactory.Create(contentType: "image/png");
        var file = StoredFileFactory.Create(contentType: "image/jpeg");

        // Act
        var result = await StoredFileClaim.EnsureClaimableAsync(_dbContext, file, currentFile, NotUploaded, ct);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "EnsureClaimableAsync succeeds when the file matches a pending upload")]
    public async Task EnsureClaimableAsync_ShouldSucceed_WhenFileMatchesPendingUpload()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var file = StoredFileFactory.Create(container: "uploads", path: "staged.png");
        await _dbContext.PendingUploads.AddAsync(PendingUploadFactory.Create(container: "uploads", path: "staged.png"), ct);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var result = await StoredFileClaim.EnsureClaimableAsync(_dbContext, file, null, NotUploaded, ct);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "EnsureClaimableAsync returns NotUploaded when the file has no pending upload")]
    public async Task EnsureClaimableAsync_ShouldReturnNotUploaded_WhenNoPendingUpload()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var file = StoredFileFactory.Create(container: "uploads", path: "arbitrary.png");

        // Act
        var result = await StoredFileClaim.EnsureClaimableAsync(_dbContext, file, null, NotUploaded, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(NotUploaded);
    }

    [Fact(DisplayName = "EnsureClaimableAsync returns NotUploaded when only the path matches a pending upload")]
    public async Task EnsureClaimableAsync_ShouldReturnNotUploaded_WhenOnlyPathMatches()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _dbContext.PendingUploads.AddAsync(PendingUploadFactory.Create(container: "uploads", path: "staged.png"), ct);
        await _dbContext.SaveChangesAsync(ct);
        var file = StoredFileFactory.Create(container: "other-container", path: "staged.png");

        // Act
        var result = await StoredFileClaim.EnsureClaimableAsync(_dbContext, file, null, NotUploaded, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(NotUploaded);
    }
}