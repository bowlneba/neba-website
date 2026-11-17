using Microsoft.EntityFrameworkCore;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Features.Series.DeleteSeries;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Uploads;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Storage;

namespace Neba.Api.Tests.Features.Series.DeleteSeries;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class DeleteSeriesCommandHandlerTests(AppDbContextFixture fixture)
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

    [Fact(DisplayName = "HandleAsync should return Deleted when the Series no longer exists")]
    public async Task HandleAsync_ShouldReturnDeleted_WhenSeriesIsMissing()
    {
        // Arrange (a strict scheduler with no setup fails the test if it is called)
        var handler = new DeleteSeriesCommandHandler(_dbContext, new Mock<IBackgroundJobScheduler>(MockBehavior.Strict).Object);

        // Act
        var result = await handler.HandleAsync(new DeleteSeriesCommand { SeriesId = SeriesId.New() }, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync should return a Locked Conflict and keep the Series when it is locked")]
    public async Task HandleAsync_ShouldReturnLocked_WhenSeriesIsLocked()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var series = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct, status: StageStatus.Computed);
        _dbContext.ChangeTracker.Clear();
        var handler = new DeleteSeriesCommandHandler(_dbContext, new Mock<IBackgroundJobScheduler>(MockBehavior.Strict).Object);

        // Act
        var result = await handler.HandleAsync(new DeleteSeriesCommand { SeriesId = series.Id }, ct);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Locked");
        result.FirstError.Type.ShouldBe(ErrorOr.ErrorType.Conflict);
        (await _dbContext.Series.CountAsync(ct)).ShouldBe(1);
    }

    [Fact(DisplayName = "HandleAsync should delete the Series and its Stages and schedule deletion of its logo")]
    public async Task HandleAsync_ShouldDeleteSeriesAndScheduleLogoDeletion()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var logo = StoredFileFactory.Create(container: "series", path: "logo.png");
        var series = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct, logo: logo);
        _dbContext.ChangeTracker.Clear();

        var scheduler = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        scheduler.Setup(s => s.Enqueue(It.Is<DeleteStoredFilesJob>(job => job.Files.Single().Container == "series" && job.Files.Single().Path == "logo.png")))
            .Returns("job");
        var handler = new DeleteSeriesCommandHandler(_dbContext, scheduler.Object);

        // Act
        var result = await handler.HandleAsync(new DeleteSeriesCommand { SeriesId = series.Id }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        scheduler.VerifyAll();
        (await _dbContext.Series.CountAsync(ct)).ShouldBe(0);
        (await _dbContext.Database.SqlQueryRaw<int>("SELECT count(*)::int AS \"Value\" FROM app.series_stages").SingleAsync(ct)).ShouldBe(0);
    }

    [Fact(DisplayName = "HandleAsync should delete a Series with no logo without scheduling any job")]
    public async Task HandleAsync_ShouldNotScheduleJob_WhenSeriesHasNoLogo()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var series = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct);
        _dbContext.ChangeTracker.Clear();
        var handler = new DeleteSeriesCommandHandler(_dbContext, new Mock<IBackgroundJobScheduler>(MockBehavior.Strict).Object);

        // Act
        var result = await handler.HandleAsync(new DeleteSeriesCommand { SeriesId = series.Id }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        (await _dbContext.Series.CountAsync(ct)).ShouldBe(0);
    }
}
