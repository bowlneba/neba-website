using Neba.Api.Database;
using Neba.Api.Features.Tournaments;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Storage;
using Neba.TestFactory.Uploads;

namespace Neba.Api.Tests.Features.Tournaments;

[IntegrationTest]
[Component("Tournaments")]
[Collection<AppDbContextFixture>]
public sealed class TournamentLogoClaimEnsureClaimableTests(AppDbContextFixture fixture)
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

    [Fact(DisplayName = "EnsureClaimableAsync succeeds when the logo is null")]
    public async Task EnsureClaimableAsync_ShouldSucceed_WhenLogoIsNull()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        // Act
        var result = await TournamentLogoClaim.EnsureClaimableAsync(_dbContext, null, StoredFileFactory.Create(), ct);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "EnsureClaimableAsync succeeds when the logo is the tournament's current logo")]
    public async Task EnsureClaimableAsync_ShouldSucceed_WhenLogoIsCurrentLogo()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var currentLogo = StoredFileFactory.Create(contentType: "image/png");
        var logo = StoredFileFactory.Create(contentType: "image/jpeg");

        // Act
        var result = await TournamentLogoClaim.EnsureClaimableAsync(_dbContext, logo, currentLogo, ct);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "EnsureClaimableAsync succeeds when the logo matches a pending upload")]
    public async Task EnsureClaimableAsync_ShouldSucceed_WhenLogoMatchesPendingUpload()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var logo = StoredFileFactory.Create(container: "uploads", path: "staged.png");
        await _dbContext.PendingUploads.AddAsync(PendingUploadFactory.Create(container: "uploads", path: "staged.png"), ct);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var result = await TournamentLogoClaim.EnsureClaimableAsync(_dbContext, logo, null, ct);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "EnsureClaimableAsync returns LogoNotUploaded when the logo has no pending upload")]
    public async Task EnsureClaimableAsync_ShouldReturnLogoNotUploaded_WhenNoPendingUpload()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var logo = StoredFileFactory.Create(container: "uploads", path: "arbitrary.png");

        // Act
        var result = await TournamentLogoClaim.EnsureClaimableAsync(_dbContext, logo, null, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(TournamentErrors.LogoNotUploaded);
    }

    [Fact(DisplayName = "EnsureClaimableAsync returns LogoNotUploaded when only the path matches a pending upload")]
    public async Task EnsureClaimableAsync_ShouldReturnLogoNotUploaded_WhenOnlyPathMatches()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _dbContext.PendingUploads.AddAsync(PendingUploadFactory.Create(container: "uploads", path: "staged.png"), ct);
        await _dbContext.SaveChangesAsync(ct);
        var logo = StoredFileFactory.Create(container: "other-container", path: "staged.png");

        // Act
        var result = await TournamentLogoClaim.EnsureClaimableAsync(_dbContext, logo, null, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(TournamentErrors.LogoNotUploaded);
    }
}
