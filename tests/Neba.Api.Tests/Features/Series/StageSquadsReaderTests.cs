using Neba.Api.Database;
using Neba.Api.Features.Series;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Series;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class StageSquadsReaderTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private static readonly DateTimeOffset Day1Morning = new(2025, 10, 4, 13, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Day1Evening = new(2025, 10, 4, 22, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Day2Morning = new(2025, 10, 5, 13, 0, 0, TimeSpan.Zero);

    private readonly AppDbContext _dbContext = fixture.CreateDbContext();

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    private static Squad[] Squads(params DateTimeOffset[] times)
        => [.. times.Select(time => SquadFactory.Create(bowlingDateTime: time))];

    [Fact(DisplayName = "ReadForTournamentAsync should order squads by bowling date and time")]
    public async Task ReadForTournamentAsync_ShouldOrderByBowlingDateTime()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var tournament = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "Squads", new DateOnly(2025, 10, 4), new DateOnly(2025, 10, 5), ct,
            squads: Squads(Day2Morning, Day1Evening, Day1Morning));

        // Act
        var rows = await StageSquadsReader.ReadForTournamentAsync(_dbContext, tournament.Id, ct);

        // Assert
        rows.Select(row => row.BowlingDateTimeUtc).ShouldBe([Day1Morning, Day1Evening, Day2Morning]);
        rows.Select(row => row.SquadId).ShouldBe(
            tournament.Squads.OrderBy(squad => squad.BowlingDateTimeUtc).Select(squad => squad.Id));
    }

    [Fact(DisplayName = "ReadForTournamentAsync should flag HasScores only for squads with score rows")]
    public async Task ReadForTournamentAsync_ShouldFlagSquadsWithScores()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var bowlers = await AdvancementReaderSeed.AddBowlersAsync(_dbContext, 2, ct);
        var tournament = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "Squads", new DateOnly(2025, 10, 4), new DateOnly(2025, 10, 5), ct,
            squads: Squads(Day1Morning, Day1Evening));
        var otherTournament = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "Other", new DateOnly(2025, 11, 4), new DateOnly(2025, 11, 5), ct,
            squads: Squads(new DateTimeOffset(2025, 11, 4, 13, 0, 0, TimeSpan.Zero)));
        var scored = tournament.Squads.Single(squad => squad.BowlingDateTimeUtc == Day1Morning);
        var unscored = tournament.Squads.Single(squad => squad.BowlingDateTimeUtc == Day1Evening);
        _dbContext.SquadScores.AddRange(
            SquadScoreFactory.Create(scored.Id, bowlers[0].Id, gameNumber: 1),
            SquadScoreFactory.Create(scored.Id, bowlers[0].Id, gameNumber: 2),
            SquadScoreFactory.Create(scored.Id, bowlers[1].Id, gameNumber: 1),
            SquadScoreFactory.Create(otherTournament.Squads.Single().Id, bowlers[0].Id, gameNumber: 1));
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var rows = await StageSquadsReader.ReadForTournamentAsync(_dbContext, tournament.Id, ct);

        // Assert
        rows.Count.ShouldBe(2);
        rows.Single(row => row.SquadId == scored.Id).HasScores.ShouldBeTrue();
        rows.Single(row => row.SquadId == unscored.Id).HasScores.ShouldBeFalse();
    }

    [Fact(DisplayName = "ReadForTournamentAsync should return an empty list for a tournament with no squads and for an unknown tournament")]
    public async Task ReadForTournamentAsync_ShouldReturnEmptyList_WhenThereAreNoSquads()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var tournament = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "No squads", new DateOnly(2025, 10, 4), null, ct);

        // Act
        var none = await StageSquadsReader.ReadForTournamentAsync(_dbContext, tournament.Id, ct);
        var unknown = await StageSquadsReader.ReadForTournamentAsync(_dbContext, TournamentId.New(), ct);

        // Assert
        none.ShouldBeEmpty();
        unknown.ShouldBeEmpty();
    }

    [Fact(DisplayName = "ReadForStageAsync should return the squads of the Stage's tournament only")]
    public async Task ReadForStageAsync_ShouldReturnTheStageTournamentSquads()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var qualifier = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "Qualifier", new DateOnly(2025, 2, 1), null, ct,
            squads: Squads(new DateTimeOffset(2025, 2, 1, 14, 0, 0, TimeSpan.Zero)));
        var survivor = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "Survivor", new DateOnly(2025, 10, 4), new DateOnly(2025, 10, 5), ct,
            squads: Squads(Day2Morning, Day1Morning));
        var placement = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "Placement", new DateOnly(2025, 12, 6), null, ct);
        var series = await AdvancementReaderSeed.AddSeriesAsync(_dbContext, season, qualifier, survivor, placement, ct);
        var survivorStage = series.Stages.Single(stage => stage.Kind == StageKind.Survivor);

        // Act
        var rows = await StageSquadsReader.ReadForStageAsync(_dbContext, series.Id, survivorStage.Id, ct);

        // Assert
        rows.ShouldNotBeNull();
        rows.Select(row => row.SquadId).ShouldBe(
            survivor.Squads.OrderBy(squad => squad.BowlingDateTimeUtc).Select(squad => squad.Id));
    }

    [Fact(DisplayName = "ReadForStageAsync should return null for an unknown Series, an unknown Stage, and a Stage of another Series")]
    public async Task ReadForStageAsync_ShouldReturnNull_WhenSeriesOrStageIsUnknown()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var first = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct, name: "First");
        var second = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 6, 1), ct, name: "Second");
        var firstStage = first.Stages.First();

        // Act
        var unknownSeries = await StageSquadsReader.ReadForStageAsync(_dbContext, SeriesId.New(), firstStage.Id, ct);
        var unknownStage = await StageSquadsReader.ReadForStageAsync(_dbContext, first.Id, SeriesStageId.New(), ct);
        var otherSeriesStage = await StageSquadsReader.ReadForStageAsync(_dbContext, second.Id, firstStage.Id, ct);

        // Assert
        unknownSeries.ShouldBeNull();
        unknownStage.ShouldBeNull();
        otherSeriesStage.ShouldBeNull();
    }

    [Fact(DisplayName = "ReadForStageAsync should return an empty list, not null, when the Stage's tournament has no squads")]
    public async Task ReadForStageAsync_ShouldReturnEmptyList_WhenTournamentHasNoSquads()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var series = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct);

        // Act
        var rows = await StageSquadsReader.ReadForStageAsync(_dbContext, series.Id, series.Stages.First().Id, ct);

        // Assert
        rows.ShouldNotBeNull();
        rows.ShouldBeEmpty();
    }
}