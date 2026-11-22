using Neba.Api.Database;
using Neba.Api.Features.Series;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;

namespace Neba.Api.Tests.Features.Series;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class StageResultRowsReaderTests(AppDbContextFixture fixture)
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

    [Fact(DisplayName = "ReadAsync should order rows by place, then last name, then first name")]
    public async Task ReadAsync_ShouldOrderByPlaceThenName()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var smithZoe = BowlerFactory.Create(name: NameFactory.Create(firstName: "Zoe", lastName: "Smith"));
        var adamsBob = BowlerFactory.Create(name: NameFactory.Create(firstName: "Bob", lastName: "Adams"));
        var smithAlan = BowlerFactory.Create(name: NameFactory.Create(firstName: "Alan", lastName: "Smith"));
        var brownCarl = BowlerFactory.Create(name: NameFactory.Create(firstName: "Carl", lastName: "Brown"));
        var lastPlace = BowlerFactory.Create(name: NameFactory.Create(firstName: "Aaron", lastName: "Aaronson"));
        _dbContext.Bowlers.AddRange(smithZoe, adamsBob, smithAlan, brownCarl, lastPlace);
        await _dbContext.SaveChangesAsync(ct);
        var qualifier = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "Qualifier", new DateOnly(2025, 2, 1), null, ct,
            status: TournamentStatus.Completed,
            results:
            [
                AdvancementReaderSeed.Result(lastPlace, 3),
                AdvancementReaderSeed.Result(smithZoe, 1),
                AdvancementReaderSeed.Result(brownCarl, 2),
                AdvancementReaderSeed.Result(adamsBob, 1),
                AdvancementReaderSeed.Result(smithAlan, 1),
            ]);
        var survivor = await AdvancementReaderSeed.AddTournamentAsync(_dbContext, season, "Survivor", new DateOnly(2025, 3, 1), null, ct);
        var placement = await AdvancementReaderSeed.AddTournamentAsync(_dbContext, season, "Placement", new DateOnly(2025, 4, 1), null, ct);
        var series = await AdvancementReaderSeed.AddSeriesAsync(_dbContext, season, qualifier, survivor, placement, ct);
        var qualifyingStage = series.Stages.Single(stage => stage.Kind == StageKind.Qualifying);

        // Act
        var rows = await StageResultRowsReader.ReadAsync(_dbContext, series.Id, qualifyingStage.Id, ct);

        // Assert
        rows.ShouldNotBeNull();
        rows.Select(row => (row.BowlerId, row.Place)).ShouldBe(
        [
            (adamsBob.Id, 1),
            (smithAlan.Id, 1),
            (smithZoe.Id, 1),
            (brownCarl.Id, 2),
            (lastPlace.Id, 3),
        ]);
        rows[0].BowlerName.FirstName.ShouldBe("Bob");
        rows[0].BowlerName.LastName.ShouldBe("Adams");
    }

    [Fact(DisplayName = "ReadAsync should return an empty list when the Stage's tournament has no results")]
    public async Task ReadAsync_ShouldReturnEmptyList_WhenTournamentHasNoResults()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var series = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct);

        // Act
        var rows = await StageResultRowsReader.ReadAsync(_dbContext, series.Id, series.Stages.First().Id, ct);

        // Assert
        rows.ShouldNotBeNull();
        rows.ShouldBeEmpty();
    }

    [Fact(DisplayName = "ReadAsync should return only the results of the requested Stage's tournament")]
    public async Task ReadAsync_ShouldReturnOnlyTheStageTournamentResults()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var bowlers = await AdvancementReaderSeed.AddBowlersAsync(_dbContext, 2, ct);
        var qualifier = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "Qualifier", new DateOnly(2025, 2, 1), null, ct,
            status: TournamentStatus.Completed, results: [AdvancementReaderSeed.Result(bowlers[0], 1)]);
        var survivor = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "Survivor", new DateOnly(2025, 3, 1), null, ct,
            status: TournamentStatus.Completed, results: [AdvancementReaderSeed.Result(bowlers[1], 1)]);
        var placement = await AdvancementReaderSeed.AddTournamentAsync(_dbContext, season, "Placement", new DateOnly(2025, 4, 1), null, ct);
        var series = await AdvancementReaderSeed.AddSeriesAsync(_dbContext, season, qualifier, survivor, placement, ct);
        var survivorStage = series.Stages.Single(stage => stage.Kind == StageKind.Survivor);

        // Act
        var rows = await StageResultRowsReader.ReadAsync(_dbContext, series.Id, survivorStage.Id, ct);

        // Assert
        rows.ShouldNotBeNull();
        rows.Select(row => row.BowlerId).ShouldBe([bowlers[1].Id]);
    }

    [Fact(DisplayName = "ReadAsync should return null for an unknown Series, an unknown Stage, and a Stage of another Series")]
    public async Task ReadAsync_ShouldReturnNull_WhenSeriesOrStageIsUnknown()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var first = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct, name: "First");
        var second = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 6, 1), ct, name: "Second");
        var firstStage = first.Stages.First();

        // Act
        var unknownSeries = await StageResultRowsReader.ReadAsync(_dbContext, SeriesId.New(), firstStage.Id, ct);
        var unknownStage = await StageResultRowsReader.ReadAsync(_dbContext, first.Id, SeriesStageId.New(), ct);
        var otherSeriesStage = await StageResultRowsReader.ReadAsync(_dbContext, second.Id, firstStage.Id, ct);

        // Assert
        unknownSeries.ShouldBeNull();
        unknownStage.ShouldBeNull();
        otherSeriesStage.ShouldBeNull();
    }

    [Fact(DisplayName = "ReadForTournamentsAsync should group places by tournament in place order and skip tournaments not asked for")]
    public async Task ReadForTournamentsAsync_ShouldGroupPlacesByTournament()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var first = BowlerFactory.Create();
        var second = BowlerFactory.Create();
        _dbContext.Bowlers.AddRange(first, second);
        await _dbContext.SaveChangesAsync(ct);
        var asked = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "Asked", new DateOnly(2025, 2, 1), null, ct,
            status: TournamentStatus.Completed,
            results: [AdvancementReaderSeed.Result(second, 2), AdvancementReaderSeed.Result(first, 1)]);
        var other = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "Other", new DateOnly(2025, 3, 1), null, ct,
            status: TournamentStatus.Completed,
            results: [AdvancementReaderSeed.Result(first, 1)]);
        var empty = await AdvancementReaderSeed.AddTournamentAsync(_dbContext, season, "Empty", new DateOnly(2025, 4, 1), null, ct);

        // Act
        var rows = await StageResultRowsReader.ReadForTournamentsAsync(_dbContext, [asked.Id, empty.Id], ct);

        // Assert
        rows[asked.Id].Select(row => (row.BowlerId, row.Place)).ShouldBe([(first.Id, 1), (second.Id, 2)]);
        rows[empty.Id].ShouldBeEmpty();
        rows[other.Id].ShouldBeEmpty();
    }
}
