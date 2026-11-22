using ErrorOr;

using Neba.Api.Database;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.ListStageResults;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;

namespace Neba.Api.Tests.Features.Series.ListStageResults;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class ListStageResultsQueryHandlerTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();
    private readonly AppDbContext _readContext = fixture.CreateDbContext();

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _readContext.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    private Task<ErrorOr<IReadOnlyList<StageResultSummaryDto>>> ReadAsync(SeriesId seriesId, SeriesStageId stageId)
        => new ListStageResultsQueryHandler(_readContext).HandleAsync(
            new ListStageResultsQuery { SeriesId = seriesId, StageId = stageId }, TestContext.Current.CancellationToken);

    [Fact(DisplayName = "HandleAsync should return NotFound for an unknown Series, an unknown Stage and a Stage of another Series")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenSeriesOrStageIsUnknown()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var first = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct, name: "First");
        var second = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 6, 1), ct, name: "Second");
        var firstStage = first.Stages.First();

        // Act
        var unknownSeries = await ReadAsync(SeriesId.New(), firstStage.Id);
        var unknownStage = await ReadAsync(first.Id, SeriesStageId.New());
        var otherSeriesStage = await ReadAsync(second.Id, firstStage.Id);

        // Assert
        foreach (var result in new[] { unknownSeries, unknownStage, otherSeriesStage })
        {
            result.IsError.ShouldBeTrue();
            result.FirstError.Type.ShouldBe(ErrorType.NotFound);
            result.FirstError.Code.ShouldBe("Series.Advancement.StageNotFound");
        }
    }

    [Fact(DisplayName = "HandleAsync should return an empty list, not an error, when the Stage's tournament has no results")]
    public async Task HandleAsync_ShouldReturnEmptyList_WhenTournamentHasNoResults()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var series = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct);

        // Act
        var result = await ReadAsync(series.Id, series.Stages.First().Id);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should order rows by place, then last name, then first name, and return the display name")]
    public async Task HandleAsync_ShouldOrderRowsAndReturnDisplayName()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var smithZoe = BowlerFactory.Create(name: NameFactory.Create(firstName: "Zoe", lastName: "Smith"));
        var adamsBob = BowlerFactory.Create(name: NameFactory.Create(firstName: "Robert", lastName: "Adams", nickname: "Bob"));
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
        var result = await ReadAsync(series.Id, qualifyingStage.Id);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Select(row => (row.BowlerId, row.Place)).ShouldBe(
        [
            (adamsBob.Id, 1),
            (smithAlan.Id, 1),
            (smithZoe.Id, 1),
            (brownCarl.Id, 2),
            (lastPlace.Id, 3),
        ]);
        result.Value.Select(row => row.BowlerName).ShouldBe(["Bob Adams", "Alan Smith", "Zoe Smith", "Carl Brown", "Aaron Aaronson"]);
    }
}