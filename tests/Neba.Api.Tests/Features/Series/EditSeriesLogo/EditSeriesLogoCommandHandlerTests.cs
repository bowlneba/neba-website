using Microsoft.EntityFrameworkCore;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.EditSeriesLogo;
using Neba.Api.Uploads;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Storage;
using Neba.TestFactory.Uploads;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Features.Series.EditSeriesLogo;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class EditSeriesLogoCommandHandlerTests(AppDbContextFixture fixture)
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

    private static EditSeriesLogoCommandHandler CreateHandler(
        AppDbContext db,
        IBackgroundJobScheduler? scheduler = null,
        Mock<ZiggyCreatures.Caching.Fusion.IFusionCache>? cache = null)
        => new(db, scheduler ?? new Mock<IBackgroundJobScheduler>(MockBehavior.Strict).Object, (cache ?? SeriesCache.Evicting()).Object);

    private async Task<SeriesAggregate> SeedAsync(Neba.Api.Features.Storage.Domain.StoredFile? logo = null, StageStatus? status = null)
    {
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var series = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct, logo: logo, status: status);
        _dbContext.ChangeTracker.Clear();
        return series;
    }

    private async Task StageAsync(string path)
    {
        _dbContext.PendingUploads.Add(PendingUploadFactory.Create(container: "series", path: path));
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact(DisplayName = "HandleAsync should return NotFound when the Series does not exist")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenSeriesIsMissing()
    {
        // Arrange
        var command = new EditSeriesLogoCommand { SeriesId = SeriesId.New() };

        // Act
        var result = await CreateHandler(_dbContext).HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.FirstError.Code.ShouldBe("Series.NotFound");
    }

    [Fact(DisplayName = "HandleAsync should return LogoNotUploaded when the file was not staged")]
    public async Task HandleAsync_ShouldReturnLogoNotUploaded_WhenNotStaged()
    {
        // Arrange
        var series = await SeedAsync();
        var command = new EditSeriesLogoCommand { SeriesId = series.Id, Logo = StoredFileFactory.Create(container: "series", path: "new.png") };

        // Act
        var result = await CreateHandler(_dbContext).HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Logo.NotUploaded");
    }

    [Fact(DisplayName = "HandleAsync should set a staged logo and remove its pending upload")]
    public async Task HandleAsync_ShouldSetStagedLogo()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var series = await SeedAsync();
        await StageAsync("new.png");
        var command = new EditSeriesLogoCommand { SeriesId = series.Id, Logo = StoredFileFactory.Create(container: "series", path: "new.png") };

        // Act
        var result = await CreateHandler(_dbContext).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        _dbContext.ChangeTracker.Clear();
        (await _dbContext.Series.AsNoTracking().SingleAsync(ct)).Logo!.Path.ShouldBe("new.png");
        (await _dbContext.PendingUploads.CountAsync(ct)).ShouldBe(0);
    }

    [Fact(DisplayName = "HandleAsync should clear the tournament cache tag once after a successful save")]
    public async Task HandleAsync_ShouldEvictTournamentCache_AfterSave()
    {
        // Arrange
        var series = await SeedAsync();
        var cache = SeriesCache.Evicting();

        // Act
        var result = await CreateHandler(_dbContext, cache: cache).HandleAsync(
            new EditSeriesLogoCommand { SeriesId = series.Id }, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        SeriesCache.VerifyEvictedOnce(cache);
    }

    [Fact(DisplayName = "HandleAsync should not touch the tournament cache when the file was not staged")]
    public async Task HandleAsync_ShouldNotEvictTournamentCache_WhenNotStaged()
    {
        // Arrange
        var series = await SeedAsync();
        var command = new EditSeriesLogoCommand { SeriesId = series.Id, Logo = StoredFileFactory.Create(container: "series", path: "new.png") };

        // Act (an untouched strict cache fails the test if it is called)
        var result = await CreateHandler(_dbContext, cache: SeriesCache.Untouched()).HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
    }

    [Fact(DisplayName = "HandleAsync should replace the logo and schedule deletion of the old file, even while locked")]
    public async Task HandleAsync_ShouldReplaceLogo_AndScheduleDeletion_WhenLocked()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var series = await SeedAsync(StoredFileFactory.Create(container: "series", path: "old.png"), StageStatus.Computed);
        await StageAsync("new.png");
        var scheduler = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        scheduler.Setup(s => s.Enqueue(It.Is<DeleteStoredFilesJob>(job => job.Files.Single().Path == "old.png"))).Returns("job");
        var command = new EditSeriesLogoCommand { SeriesId = series.Id, Logo = StoredFileFactory.Create(container: "series", path: "new.png") };

        // Act
        var result = await CreateHandler(_dbContext, scheduler.Object).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        scheduler.VerifyAll();
        _dbContext.ChangeTracker.Clear();
        (await _dbContext.Series.AsNoTracking().SingleAsync(ct)).Logo!.Path.ShouldBe("new.png");
    }

    [Fact(DisplayName = "HandleAsync should remove the logo and schedule deletion when the command's logo is null")]
    public async Task HandleAsync_ShouldRemoveLogo_AndScheduleDeletion()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var series = await SeedAsync(StoredFileFactory.Create(container: "series", path: "old.png"));
        var scheduler = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        scheduler.Setup(s => s.Enqueue(It.Is<DeleteStoredFilesJob>(job => job.Files.Single().Path == "old.png"))).Returns("job");

        // Act
        var result = await CreateHandler(_dbContext, scheduler.Object).HandleAsync(new EditSeriesLogoCommand { SeriesId = series.Id }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        scheduler.VerifyAll();
        _dbContext.ChangeTracker.Clear();
        (await _dbContext.Series.AsNoTracking().SingleAsync(ct)).Logo.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should keep the stored file and schedule nothing when the same file is resubmitted")]
    public async Task HandleAsync_ShouldKeepStoredFile_WhenSameFileIsResubmitted()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var stored = StoredFileFactory.Create(container: "series", path: "same.png", contentType: "image/png", sizeInBytes: 50);
        var series = await SeedAsync(stored);
        var resubmitted = StoredFileFactory.Create(container: "series", path: "same.png", contentType: "image/jpeg", sizeInBytes: 999);

        // Act (a strict scheduler with no setup fails if a job is scheduled)
        var result = await CreateHandler(_dbContext).HandleAsync(new EditSeriesLogoCommand { SeriesId = series.Id, Logo = resubmitted }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        _dbContext.ChangeTracker.Clear();
        var saved = (await _dbContext.Series.AsNoTracking().SingleAsync(ct)).Logo!;
        saved.ContentType.ShouldBe("image/png");
        saved.SizeInBytes.ShouldBe(50);
    }

    [Fact(DisplayName = "HandleAsync should return ConcurrentChange and keep the old logo when a job moves Version between the load and the save")]
    public async Task HandleAsync_ShouldReturnConcurrentChange_WhenVersionMovesBeforeSave()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var series = await SeedAsync(StoredFileFactory.Create(container: "series", path: "old.png"));
        await StageAsync("new.png");
        var job = new BeforeSaveInterceptor(async token =>
        {
            await using var jobContext = fixture.CreateDbContext();
            await jobContext.Series.ExecuteUpdateAsync(set => set.SetProperty(s => s.Version, s => s.Version + 1), token);
        });
        await using var db = fixture.CreateDbContext(job);
        var command = new EditSeriesLogoCommand { SeriesId = series.Id, Logo = StoredFileFactory.Create(container: "series", path: "new.png") };

        // Act (a strict scheduler and cache with no setup fail if the handler goes on after the conflict)
        var result = await CreateHandler(db, cache: SeriesCache.Untouched()).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeriesAdvancementErrors.ConcurrentChange);
        await using var read = fixture.CreateDbContext();
        var stored = await read.Series.AsNoTracking().SingleAsync(ct);
        stored.Logo!.Path.ShouldBe("old.png");
        stored.Version.ShouldBe(1);
        (await read.PendingUploads.CountAsync(ct)).ShouldBe(1);
    }
}