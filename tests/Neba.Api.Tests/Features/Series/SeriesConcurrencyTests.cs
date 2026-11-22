using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Series;
using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class SeriesConcurrencyTests(AppDbContextFixture fixture)
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

    private async Task<SeriesId> SeedAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var series = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct, name: "VH");
        _dbContext.ChangeTracker.Clear();

        return series.Id;
    }

    /// <summary>What an advancement job does to the Series row: a change that moves Version.</summary>
    private async Task BumpVersionAsync(SeriesId seriesId)
    {
        await using var job = fixture.CreateDbContext();
        await job.Series.Where(series => series.Id == seriesId)
            .ExecuteUpdateAsync(set => set.SetProperty(series => series.Version, series => series.Version + 1), TestContext.Current.CancellationToken);
    }

    [Fact(DisplayName = "SaveOrConflictAsync should save and return success when nothing else changed the Series")]
    public async Task SaveOrConflictAsync_ShouldSave_WhenThereIsNoConflict()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var seriesId = await SeedAsync();
        var series = await _dbContext.Series.SingleAsync(s => s.Id == seriesId, ct);
        series.Rename("Renamed").IsError.ShouldBeFalse();

        // Act
        var result = await SeriesConcurrency.SaveOrConflictAsync(_dbContext, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        await using var read = fixture.CreateDbContext();
        (await read.Series.AsNoTracking().SingleAsync(s => s.Id == seriesId, ct)).Name.ShouldBe("Renamed");
    }

    [Fact(DisplayName = "SaveOrConflictAsync should return ConcurrentChange when a job moved Version after the Series loaded")]
    public async Task SaveOrConflictAsync_ShouldReturnConcurrentChange_WhenVersionMovedAfterLoad()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var seriesId = await SeedAsync();
        var series = await _dbContext.Series.SingleAsync(s => s.Id == seriesId, ct);
        await BumpVersionAsync(seriesId);
        series.Rename("Stale write").IsError.ShouldBeFalse();

        // Act
        var result = await SeriesConcurrency.SaveOrConflictAsync(_dbContext, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeriesAdvancementErrors.ConcurrentChange);
        await using var read = fixture.CreateDbContext();
        var stored = await read.Series.AsNoTracking().SingleAsync(s => s.Id == seriesId, ct);
        stored.Name.ShouldBe("VH");
        stored.Version.ShouldBe(1);
    }

    [Fact(DisplayName = "SaveOrConflictAsync should return SaveConflict when a Stage tournament already belongs to another Series")]
    public async Task SaveOrConflictAsync_ShouldReturnSaveConflict_WhenTournamentIsInAnotherSeries()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var seriesId = await SeedAsync();
        var season = await _dbContext.Seasons.SingleAsync(ct);
        var taken = await _dbContext.Tournaments.OrderBy(t => t.StartDate).Take(3).ToListAsync(ct);
        var duplicate = SeriesFactory.CreateFor(
            season,
            [
                (taken[0], StageKind.Qualifying, 8, null, null),
                (taken[1], StageKind.Survivor, 20, taken[1].StartDate, new TimeOnly(9, 0)),
                (taken[2], StageKind.Placement, null, null, null),
            ],
            name: "Race");
        _dbContext.Series.Add(duplicate);

        // Act
        var result = await SeriesConcurrency.SaveOrConflictAsync(_dbContext, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(SeriesAdvancementErrors.SaveConflict);
        await using var read = fixture.CreateDbContext();
        (await read.Series.AsNoTracking().CountAsync(ct)).ShouldBe(1);
        (await read.Series.AsNoTracking().SingleAsync(ct)).Id.ShouldBe(seriesId);
    }
}