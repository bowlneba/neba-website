using Microsoft.EntityFrameworkCore;

using Neba.Api.Contracts.Memberships;
using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Series;
using Neba.TestFactory.Tournaments;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Features.Series;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class SeriesAdvancementFactsReaderTests(AppDbContextFixture fixture)
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

    private async Task<AdvancementFacts> ReadAsync(SeriesId seriesId)
    {
        var ct = TestContext.Current.CancellationToken;
        _dbContext.ChangeTracker.Clear();

        var series = await _readContext.Series
            .Include(s => s.Stages)
            .Include(s => s.FieldEntries)
            .AsSplitQuery()
            .SingleAsync(s => s.Id == seriesId, ct);

        return await new SeriesAdvancementFactsReader(_readContext).ReadAsync(series, ct);
    }

    private async Task SetStatusAsync(TournamentId tournamentId, TournamentStatus status)
        => await _dbContext.Tournaments.Where(t => t.Id == tournamentId)
            .ExecuteUpdateAsync(set => set.SetProperty(t => t.Status, status), TestContext.Current.CancellationToken);

    private static IReadOnlyCollection<TournamentResult> Results(IReadOnlyList<Bowler> bowlers, int count)
        => [.. bowlers.Take(count).Select((bowler, index) => AdvancementReaderSeed.Result(bowler, index + 1))];

    [Theory(DisplayName = "ReadAsync should read results only for a Completed or Truncated tournament")]
    [InlineData("Scheduled", 0)]
    [InlineData("Cancelled", 0)]
    [InlineData("Completed", 3)]
    [InlineData("Truncated", 3)]
    public async Task ReadAsync_ShouldReadResultsOnlyForFinalizedTournaments(string statusName, int expectedResults)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var status = TournamentStatus.FromName(statusName);
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var bowlers = await AdvancementReaderSeed.AddBowlersAsync(_dbContext, 3, ct);
        var qualifier = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "Qualifier", new DateOnly(2025, 2, 1), null, ct,
            status: TournamentStatus.Completed, results: Results(bowlers, 3));
        var survivor = await AdvancementReaderSeed.AddTournamentAsync(_dbContext, season, "Survivor", new DateOnly(2025, 3, 3), null, ct);
        var placement = await AdvancementReaderSeed.AddTournamentAsync(_dbContext, season, "Placement", new DateOnly(2025, 4, 5), null, ct);
        var series = await AdvancementReaderSeed.AddSeriesAsync(_dbContext, season, qualifier, survivor, placement, ct);

        // The results stay in the database; only the status changes.
        await SetStatusAsync(qualifier.Id, status);

        // Act
        var facts = await ReadAsync(series.Id);

        // Assert
        var read = facts.Tournaments[qualifier.Id];
        read.Status.ShouldBe(status);
        read.StartDate.ShouldBe(qualifier.StartDate);
        read.EndDate.ShouldBe(qualifier.EndDate);
        read.Results.Count.ShouldBe(expectedResults);
        read.Results.Select(row => row.BowlerId).ShouldBe(bowlers.Take(expectedResults).Select(b => b.Id), ignoreOrder: true);
        read.Results.Select(row => row.Place).Order().ShouldBe(Enumerable.Range(1, expectedResults));
    }

    [Fact(DisplayName = "ReadAsync should return one entry for each Stage's tournament")]
    public async Task ReadAsync_ShouldReturnOneEntryPerStageTournament()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var series = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct);

        // Act
        var facts = await ReadAsync(series.Id);

        // Assert
        facts.Tournaments.Keys.ShouldBe(series.Stages.Select(stage => stage.TournamentId), ignoreOrder: true);
        facts.Tournaments.Values.ShouldAllBe(tournament => tournament.Status == TournamentStatus.Scheduled && tournament.Results.Count == 0);
    }

    [Fact(DisplayName = "ReadAsync should convert squad times to Eastern daylight time and total each bowler's scores for a Survivor Stage only")]
    public async Task ReadAsync_ShouldFillSquadsForSurvivorStageOnly_InEasternDaylightTime()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var bowlers = await AdvancementReaderSeed.AddBowlersAsync(_dbContext, 3, ct);
        var morning = new DateTimeOffset(2025, 7, 12, 13, 0, 0, TimeSpan.Zero);
        var lateEvening = new DateTimeOffset(2025, 7, 13, 2, 30, 0, TimeSpan.Zero);
        var qualifier = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "Qualifier", new DateOnly(2025, 6, 7), null, ct,
            squads: [SquadFactory.Create(new DateTimeOffset(2025, 6, 7, 14, 0, 0, TimeSpan.Zero))]);
        var survivor = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "Survivor", new DateOnly(2025, 7, 12), new DateOnly(2025, 7, 13), ct,
            squads: [SquadFactory.Create(morning), SquadFactory.Create(lateEvening)]);
        var placement = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "Placement", new DateOnly(2025, 8, 9), null, ct,
            squads: [SquadFactory.Create(new DateTimeOffset(2025, 8, 9, 14, 0, 0, TimeSpan.Zero))]);
        var series = await AdvancementReaderSeed.AddSeriesAsync(_dbContext, season, qualifier, survivor, placement, ct);
        var morningSquad = survivor.Squads.Single(squad => squad.BowlingDateTimeUtc == morning);
        var eveningSquad = survivor.Squads.Single(squad => squad.BowlingDateTimeUtc == lateEvening);
        _dbContext.SquadScores.AddRange(
            SquadScoreFactory.Create(morningSquad.Id, bowlers[0].Id, gameNumber: 1, value: 200),
            SquadScoreFactory.Create(morningSquad.Id, bowlers[0].Id, gameNumber: 2, value: 210),
            SquadScoreFactory.Create(morningSquad.Id, bowlers[0].Id, gameNumber: 3, value: 220),
            SquadScoreFactory.Create(morningSquad.Id, bowlers[1].Id, gameNumber: 1, value: 190),
            SquadScoreFactory.Create(morningSquad.Id, bowlers[1].Id, gameNumber: 2, value: 180),
            SquadScoreFactory.Create(eveningSquad.Id, bowlers[2].Id, gameNumber: 1, value: 150),
            SquadScoreFactory.Create(qualifier.Squads.Single().Id, bowlers[0].Id, gameNumber: 1, value: 250));
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var facts = await ReadAsync(series.Id);

        // Assert
        facts.Tournaments[qualifier.Id].Squads.ShouldBeEmpty();
        facts.Tournaments[placement.Id].Squads.ShouldBeEmpty();
        var squads = facts.Tournaments[survivor.Id].Squads;
        squads.Count.ShouldBe(2);
        var first = squads.Single(squad => squad.SquadId == morningSquad.Id);
        first.LocalDate.ShouldBe(new DateOnly(2025, 7, 12));
        first.LocalStartTime.ShouldBe(new TimeOnly(9, 0));
        first.Totals.ShouldBe([new SquadBowlerTotal(bowlers[0].Id, 630, 3), new SquadBowlerTotal(bowlers[1].Id, 370, 2)], ignoreOrder: true);
        var second = squads.Single(squad => squad.SquadId == eveningSquad.Id);
        second.LocalDate.ShouldBe(new DateOnly(2025, 7, 12));
        second.LocalStartTime.ShouldBe(new TimeOnly(22, 30));
        second.Totals.ShouldBe([new SquadBowlerTotal(bowlers[2].Id, 150, 1)]);
    }

    [Fact(DisplayName = "ReadAsync should convert squad times to Eastern standard time and keep a squad without scores with no totals")]
    public async Task ReadAsync_ShouldConvertToEasternStandardTime_AndKeepSquadWithoutScores()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var winter = new DateTimeOffset(2025, 12, 6, 14, 0, 0, TimeSpan.Zero);
        var qualifier = await AdvancementReaderSeed.AddTournamentAsync(_dbContext, season, "Qualifier", new DateOnly(2025, 10, 4), null, ct);
        var survivor = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "Survivor", new DateOnly(2025, 12, 6), null, ct, squads: [SquadFactory.Create(winter)]);
        var placement = await AdvancementReaderSeed.AddTournamentAsync(_dbContext, season, "Placement", new DateOnly(2026, 1, 10), null, ct);
        var series = await AdvancementReaderSeed.AddSeriesAsync(_dbContext, season, qualifier, survivor, placement, ct);

        // Act
        var facts = await ReadAsync(series.Id);

        // Assert
        var squad = facts.Tournaments[survivor.Id].Squads.Single();
        squad.SquadId.ShouldBe(survivor.Squads.Single().Id);
        squad.LocalDate.ShouldBe(new DateOnly(2025, 12, 6));
        squad.LocalStartTime.ShouldBe(new TimeOnly(9, 0));
        squad.Totals.ShouldBeEmpty();
    }

    [Fact(DisplayName = "ReadAsync should load the members on the end date of each Qualifying tournament and no other date")]
    public async Task ReadAsync_ShouldLoadMembersOnEachQualifyingEndDate()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var early = BowlerFactory.Create();
        var late = BowlerFactory.Create();
        var lapsed = BowlerFactory.Create();
        early.AddLegacyMembership(1, MembershipType.New, 25m, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31)).IsError.ShouldBeFalse();
        late.AddLegacyMembership(2, MembershipType.New, 25m, new DateOnly(2025, 3, 1), new DateOnly(2025, 12, 31)).IsError.ShouldBeFalse();
        lapsed.AddLegacyMembership(3, MembershipType.New, 25m, new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)).IsError.ShouldBeFalse();
        _dbContext.Bowlers.AddRange(early, late, lapsed);
        await _dbContext.SaveChangesAsync(ct);
        var firstQualifier = await AdvancementReaderSeed.AddTournamentAsync(_dbContext, season, "Q1", new DateOnly(2025, 2, 1), new DateOnly(2025, 2, 2), ct);
        var secondQualifier = await AdvancementReaderSeed.AddTournamentAsync(_dbContext, season, "Q2", new DateOnly(2025, 3, 8), new DateOnly(2025, 3, 9), ct);
        var survivor = await AdvancementReaderSeed.AddTournamentAsync(_dbContext, season, "Survivor", new DateOnly(2025, 4, 5), null, ct);
        var placement = await AdvancementReaderSeed.AddTournamentAsync(_dbContext, season, "Placement", new DateOnly(2025, 5, 3), null, ct);
        var series = SeriesFactory.CreateFor(
            season,
            [
                (firstQualifier, StageKind.Qualifying, 8, null, null),
                (secondQualifier, StageKind.Qualifying, 8, null, null),
                (survivor, StageKind.Survivor, 20, survivor.StartDate, new TimeOnly(9, 0)),
                (placement, StageKind.Placement, null, null, null),
            ]);
        _dbContext.Series.Add(series);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var facts = await ReadAsync(series.Id);

        // Assert
        facts.MembersOnDate.Keys.ShouldBe([new DateOnly(2025, 2, 2), new DateOnly(2025, 3, 9)], ignoreOrder: true);
        facts.MembersOn(new DateOnly(2025, 2, 2)).ShouldBe([early.Id], ignoreOrder: true);
        facts.MembersOn(new DateOnly(2025, 3, 9)).ShouldBe([early.Id, late.Id], ignoreOrder: true);
    }

    [Fact(DisplayName = "ReadAsync should set the Backfill as of the last Qualifying tournament's end date with the open points to that date")]
    public async Task ReadAsync_ShouldSetBackfillAsOfTheLastQualifyingEndDate()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var bowlers = await AdvancementReaderSeed.AddBowlersAsync(_dbContext, 4, ct);
        var firstQualifier = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "Q1", new DateOnly(2025, 2, 1), new DateOnly(2025, 2, 2), ct,
            status: TournamentStatus.Completed,
            results: [AdvancementReaderSeed.Result(bowlers[0], 1, 10), AdvancementReaderSeed.Result(bowlers[1], 2, 10)]);
        var secondQualifier = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "Q2", new DateOnly(2025, 3, 8), new DateOnly(2025, 3, 9), ct,
            status: TournamentStatus.Completed,
            results: [AdvancementReaderSeed.Result(bowlers[1], 1, 20), AdvancementReaderSeed.Result(bowlers[2], 2, 5)]);
        var survivor = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "Survivor", new DateOnly(2025, 4, 5), null, ct,
            status: TournamentStatus.Completed, results: [AdvancementReaderSeed.Result(bowlers[3], 1, 99)]);
        var placement = await AdvancementReaderSeed.AddTournamentAsync(_dbContext, season, "Placement", new DateOnly(2025, 5, 3), null, ct);
        var series = SeriesFactory.CreateFor(
            season,
            [
                (firstQualifier, StageKind.Qualifying, 8, null, null),
                (secondQualifier, StageKind.Qualifying, 8, null, null),
                (survivor, StageKind.Survivor, 20, survivor.StartDate, new TimeOnly(9, 0)),
                (placement, StageKind.Placement, null, null, null),
            ]);
        _dbContext.Series.Add(series);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var facts = await ReadAsync(series.Id);

        // Assert
        facts.Backfill.ShouldNotBeNull();
        facts.Backfill.AsOf.ShouldBe(new DateOnly(2025, 3, 9));
        facts.Backfill.Ready.ShouldBeTrue();
        facts.Backfill.Points.ShouldBe(
            [
                new BowlerPointsFact(bowlers[1].Id, 30),
                new BowlerPointsFact(bowlers[0].Id, 10),
                new BowlerPointsFact(bowlers[2].Id, 5),
            ]);
    }

    [Fact(DisplayName = "ReadAsync should return null Backfill and no tournaments, members or names for a Series without Stages")]
    public async Task ReadAsync_ShouldReturnNullBackfill_WhenSeriesHasNoStages()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var series = SeriesFactory.Create(seasonId: season.Id);
        _dbContext.Series.Add(series);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var facts = await ReadAsync(series.Id);

        // Assert
        facts.Backfill.ShouldBeNull();
        facts.Tournaments.ShouldBeEmpty();
        facts.MembersOnDate.ShouldBeEmpty();
        facts.SortNames.ShouldBeEmpty();
    }

    [Fact(DisplayName = "ReadAsync should return null Backfill and no members for a Series without a Qualifying Stage")]
    public async Task ReadAsync_ShouldReturnNullBackfill_WhenSeriesHasNoQualifyingStage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var survivor = await AdvancementReaderSeed.AddTournamentAsync(_dbContext, season, "Survivor", new DateOnly(2025, 4, 5), null, ct);
        var placement = await AdvancementReaderSeed.AddTournamentAsync(_dbContext, season, "Placement", new DateOnly(2025, 5, 3), null, ct);
        var series = SeriesFactory.CreateFor(
            season,
            [
                (survivor, StageKind.Survivor, 20, survivor.StartDate, new TimeOnly(9, 0)),
                (placement, StageKind.Placement, null, null, null),
            ]);
        _dbContext.Series.Add(series);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var facts = await ReadAsync(series.Id);

        // Assert
        facts.Backfill.ShouldBeNull();
        facts.MembersOnDate.ShouldBeEmpty();
        facts.Tournaments.Count.ShouldBe(2);
    }

    [Fact(DisplayName = "ReadAsync should return sort names for bowlers from Stage results, Backfill points and the stored field only")]
    public async Task ReadAsync_ShouldReturnSortNamesFromThreeSources()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var bowlers = await AdvancementReaderSeed.AddBowlersAsync(_dbContext, 4, ct);
        var fieldOnly = bowlers[0];
        var resultOnly = bowlers[1];
        var pointsOnly = bowlers[2];
        var unrelated = bowlers[3];
        await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "Early Open", new DateOnly(2025, 1, 11), null, ct,
            status: TournamentStatus.Completed, results: [AdvancementReaderSeed.Result(pointsOnly, 1, 50)]);
        var qualifier = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "Qualifier", new DateOnly(2025, 2, 1), null, ct,
            status: TournamentStatus.Completed, results: [AdvancementReaderSeed.Result(resultOnly, 1, 10)]);
        var survivor = await AdvancementReaderSeed.AddTournamentAsync(_dbContext, season, "Survivor", new DateOnly(2025, 3, 3), null, ct);
        var placement = await AdvancementReaderSeed.AddTournamentAsync(_dbContext, season, "Placement", new DateOnly(2025, 4, 5), null, ct);
        var series = await AdvancementReaderSeed.AddSeriesAsync(_dbContext, season, qualifier, survivor, placement, ct);
        var storedFacts = AdvancementFactsFactory.Create(
            tournaments: new Dictionary<TournamentId, StageTournamentResults>
            {
                [qualifier.Id] = StageTournamentResultsFactory.Create(
                    qualifier.Id, TournamentStatus.Completed, qualifier.StartDate, qualifier.EndDate,
                    results: [StageResultRowFactory.Create(fieldOnly.Id, 1)]),
            },
            membersOnDate: new Dictionary<DateOnly, IReadOnlySet<BowlerId>> { [qualifier.EndDate] = new HashSet<BowlerId> { fieldOnly.Id } },
            backfill: BackfillFactsFactory.Create(qualifier.EndDate, true, []));
        _ = series.Advance(qualifier.Id, storedFacts, Now);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var facts = await ReadAsync(series.Id);

        // Assert
        facts.SortNames.Keys.ShouldBe([fieldOnly.Id, resultOnly.Id, pointsOnly.Id], ignoreOrder: true);
        facts.SortNames.ContainsKey(unrelated.Id).ShouldBeFalse();
        facts.SortNames[fieldOnly.Id].ShouldBe(new BowlerSortName("Last01", "First01"));
        facts.SortNames[resultOnly.Id].ShouldBe(new BowlerSortName("Last02", "First02"));
        facts.SortNames[pointsOnly.Id].ShouldBe(new BowlerSortName("Last03", "First03"));
    }
}