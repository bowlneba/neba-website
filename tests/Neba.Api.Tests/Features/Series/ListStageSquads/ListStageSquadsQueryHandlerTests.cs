using ErrorOr;

using Neba.Api.Database;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.ListStageSquads;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Series;
using Neba.TestFactory.Tournaments;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Features.Series.ListStageSquads;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class ListStageSquadsQueryHandlerTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2025, 8, 1, 12, 0, 0, TimeSpan.Zero);

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

    private Task<ErrorOr<IReadOnlyList<StageSquadSummaryDto>>> ReadAsync(SeriesId seriesId, SeriesStageId stageId)
        => new ListStageSquadsQueryHandler(_readContext).HandleAsync(
            new ListStageSquadsQuery { SeriesId = seriesId, StageId = stageId }, TestContext.Current.CancellationToken);

    // A Series whose Survivor tournament has a squad at each given instant. The Qualifier is the first day of 2025 and the
    // Placement the last, so any instant in between is valid.
    private async Task<(SeriesAggregate Series, Tournament Survivor, SeriesStage Stage)> SeedAsync(params DateTimeOffset[] squadTimes)
    {
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var first = squadTimes.Length == 0 ? new DateOnly(2025, 6, 1) : DateOnly.FromDateTime(squadTimes.Min().UtcDateTime);
        var last = squadTimes.Length == 0 ? first : DateOnly.FromDateTime(squadTimes.Max().UtcDateTime);

        var qualifier = await AdvancementReaderSeed.AddTournamentAsync(_dbContext, season, "Qualifier", new DateOnly(2025, 1, 1), null, ct);
        var survivor = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "Survivor", first, last, ct,
            squads: [.. squadTimes.Select(time => SquadFactory.Create(bowlingDateTime: time))]);
        var placement = await AdvancementReaderSeed.AddTournamentAsync(_dbContext, season, "Placement", new DateOnly(2025, 12, 31), null, ct);
        var series = await AdvancementReaderSeed.AddSeriesAsync(_dbContext, season, qualifier, survivor, placement, ct);

        return (series, survivor, series.Stages.Single(stage => stage.Kind == StageKind.Survivor));
    }

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

    [Fact(DisplayName = "HandleAsync should return an empty list, not an error, when the Stage's tournament has no squads")]
    public async Task HandleAsync_ShouldReturnEmptyList_WhenTournamentHasNoSquads()
    {
        // Arrange
        var (series, _, stage) = await SeedAsync();

        // Act
        var result = await ReadAsync(series.Id, stage.Id);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should list squads in start order with Eastern date and start time, across the Eastern midnight boundary")]
    public async Task HandleAsync_ShouldListSquadsInStartOrderWithEasternTime()
    {
        // Arrange
        // 13:00 UTC on 10/4 is 9:00 AM EDT. 02:30 UTC on 10/5 is 10:30 PM EDT on 10/4, so its Eastern date is the day before its UTC date.
        var day1Morning = new DateTimeOffset(2025, 10, 4, 13, 0, 0, TimeSpan.Zero);
        var day1LateNight = new DateTimeOffset(2025, 10, 5, 2, 30, 0, TimeSpan.Zero);
        var day2Morning = new DateTimeOffset(2025, 10, 5, 13, 0, 0, TimeSpan.Zero);
        var (series, survivor, stage) = await SeedAsync(day2Morning, day1LateNight, day1Morning);

        // Act
        var result = await ReadAsync(series.Id, stage.Id);

        // Assert
        result.IsError.ShouldBeFalse();
        var squads = result.Value;
        squads.Select(s => s.SquadId).ShouldBe(
            [.. survivor.Squads.OrderBy(s => s.BowlingDateTimeUtc).Select(s => s.Id)]);
        squads.Select(s => s.LocalDate).ShouldBe([new DateOnly(2025, 10, 4), new DateOnly(2025, 10, 4), new DateOnly(2025, 10, 5)]);
        squads.Select(s => s.LocalStartTime).ShouldBe([new TimeOnly(9, 0), new TimeOnly(22, 30), new TimeOnly(9, 0)]);
    }

    [Theory(DisplayName = "HandleAsync should convert a UTC squad time to Eastern wall-clock time across daylight saving changes")]
    [InlineData("2025-01-15T14:00:00Z", "2025-01-15", "09:00")] // EST, UTC-5
    [InlineData("2025-07-15T13:00:00Z", "2025-07-15", "09:00")] // EDT, UTC-4
    [InlineData("2025-03-09T06:59:00Z", "2025-03-09", "01:59")] // last minute before the clocks go forward
    [InlineData("2025-03-09T07:00:00Z", "2025-03-09", "03:00")] // first minute after the clocks go forward
    [InlineData("2025-11-02T05:30:00Z", "2025-11-02", "01:30")] // first 1:30 AM, EDT
    [InlineData("2025-11-02T06:30:00Z", "2025-11-02", "01:30")] // second 1:30 AM, EST
    [InlineData("2025-11-02T04:30:00Z", "2025-11-02", "00:30")] // EDT, still before the change
    public async Task HandleAsync_ShouldConvertToEasternAcrossDaylightSaving(string utc, string expectedDate, string expectedTime)
    {
        // Arrange
        var instant = DateTimeOffset.Parse(utc, System.Globalization.CultureInfo.InvariantCulture);
        var (series, _, stage) = await SeedAsync(instant);

        // Act
        var result = await ReadAsync(series.Id, stage.Id);

        // Assert
        result.IsError.ShouldBeFalse();
        var squad = result.Value.Single();
        squad.LocalDate.ShouldBe(DateOnly.Parse(expectedDate, System.Globalization.CultureInfo.InvariantCulture));
        squad.LocalStartTime.ShouldBe(TimeOnly.Parse(expectedTime, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact(DisplayName = "HandleAsync should flag HasScores true only for squads with score rows")]
    public async Task HandleAsync_ShouldFlagHasScores()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var morning = new DateTimeOffset(2025, 10, 4, 13, 0, 0, TimeSpan.Zero);
        var evening = new DateTimeOffset(2025, 10, 4, 22, 0, 0, TimeSpan.Zero);
        var (series, survivor, stage) = await SeedAsync(morning, evening);
        var bowlers = await AdvancementReaderSeed.AddBowlersAsync(_dbContext, 1, ct);
        var scored = survivor.Squads.Single(squad => squad.BowlingDateTimeUtc == morning);
        var unscored = survivor.Squads.Single(squad => squad.BowlingDateTimeUtc == evening);
        _dbContext.SquadScores.Add(SquadScoreFactory.Create(scored.Id, bowlers[0].Id, gameNumber: 1));
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var result = await ReadAsync(series.Id, stage.Id);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Single(s => s.SquadId == scored.Id).HasScores.ShouldBeTrue();
        result.Value.Single(s => s.SquadId == unscored.Id).HasScores.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync should select only the squad the Stage designates")]
    public async Task HandleAsync_ShouldSelectOnlyTheDesignatedSquad()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var morning = new DateTimeOffset(2025, 10, 4, 13, 0, 0, TimeSpan.Zero);
        var evening = new DateTimeOffset(2025, 10, 4, 22, 0, 0, TimeSpan.Zero);
        var (series, survivor, stage) = await SeedAsync(morning, evening);
        var designated = survivor.Squads.Single(squad => squad.BowlingDateTimeUtc == evening);
        var other = survivor.Squads.Single(squad => squad.BowlingDateTimeUtc == morning);
        var facts = AdvancementFactsFactory.Create(
            tournaments: new Dictionary<TournamentId, StageTournamentResults>
            {
                [survivor.Id] = StageTournamentResultsFactory.Create(
                    survivor.Id, TournamentStatus.Scheduled,
                    squads: [StageSquadFactFactory.Create(designated.Id), StageSquadFactFactory.Create(other.Id)]),
            });
        series.PickSquad(stage.Id, designated.Id, series.Version, facts, Now).IsError.ShouldBeFalse();
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var result = await ReadAsync(series.Id, stage.Id);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Single(s => s.SquadId == designated.Id).IsSelected.ShouldBeTrue();
        result.Value.Single(s => s.SquadId == other.Id).IsSelected.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync should select no squad when the Stage designates none")]
    public async Task HandleAsync_ShouldSelectNoSquad_WhenNoneIsDesignated()
    {
        // Arrange
        var (series, _, stage) = await SeedAsync(
            new DateTimeOffset(2025, 10, 4, 13, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 10, 4, 22, 0, 0, TimeSpan.Zero));

        // Act
        var result = await ReadAsync(series.Id, stage.Id);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Count.ShouldBe(2);
        result.Value.ShouldAllBe(s => !s.IsSelected);
    }

    [Fact(DisplayName = "HandleAsync should select no squad when the designated squad no longer exists")]
    public async Task HandleAsync_ShouldSelectNoSquad_WhenDesignatedSquadVanished()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var (series, survivor, stage) = await SeedAsync(new DateTimeOffset(2025, 10, 4, 13, 0, 0, TimeSpan.Zero));
        var vanished = SquadId.New();
        var facts = AdvancementFactsFactory.Create(
            tournaments: new Dictionary<TournamentId, StageTournamentResults>
            {
                [survivor.Id] = StageTournamentResultsFactory.Create(
                    survivor.Id, TournamentStatus.Scheduled, squads: [StageSquadFactFactory.Create(vanished)]),
            });
        series.PickSquad(stage.Id, vanished, series.Version, facts, Now).IsError.ShouldBeFalse();
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var result = await ReadAsync(series.Id, stage.Id);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Count.ShouldBe(1);
        result.Value.ShouldAllBe(s => !s.IsSelected);
    }
}