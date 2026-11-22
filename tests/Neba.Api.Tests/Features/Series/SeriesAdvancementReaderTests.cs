using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Series;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Series;
using Neba.TestFactory.Tournaments;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Features.Series;

// Not covered: the retry when a write lands between the queries. ReadAsync has no seam to inject a write between its
// statements, so the loop is checked by reading only.
[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class SeriesAdvancementReaderTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2025, 8, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeOnly NineAm = new(9, 0);

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

    private sealed record Scenario(
        SeriesAggregate Series,
        IReadOnlyList<Bowler> Bowlers,
        Tournament Qualifier,
        Tournament Survivor,
        Tournament Placement,
        SeriesStage QualifyingStage,
        SeriesStage SurvivorStage,
        SeriesStage PlacementStage,
        SquadId SquadId);

    // Bowlers 1 to 10. The Qualifier ranks bowlers 1 to 9 (eight slots). The Survivor squad and the Placement round hold all ten.
    // Advancing computes all three Stages. Then bowler 3's slot is cleared (the Backfill takes bowler 10 with 77 points) and
    // bowler 9 is added as an override.
    private async Task<Scenario> SeedAdvancedSeriesAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var bowlers = await AdvancementReaderSeed.AddBowlersAsync(_dbContext, 10, ct);

        var qualifier = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "The Qualifier", new DateOnly(2025, 2, 1), null, ct,
            status: TournamentStatus.Completed,
            results: [.. bowlers.Take(9).Select((bowler, index) => AdvancementReaderSeed.Result(bowler, index + 1))]);
        var survivor = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "The Survivor", new DateOnly(2025, 3, 3), null, ct,
            status: TournamentStatus.Completed,
            results: [.. bowlers.Select((bowler, index) => AdvancementReaderSeed.Result(bowler, index + 1))],
            squads: [SquadFactory.Create(new DateTimeOffset(2025, 3, 3, 14, 0, 0, TimeSpan.Zero))]);
        var placement = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "The Placement", new DateOnly(2025, 4, 5), null, ct,
            status: TournamentStatus.Completed,
            results: [.. bowlers.Select((bowler, index) => AdvancementReaderSeed.Result(bowler, index + 1))]);

        var series = await AdvancementReaderSeed.AddSeriesAsync(_dbContext, season, qualifier, survivor, placement, ct);
        var squadId = survivor.Squads.Single().Id;

        var facts = AdvancementFactsFactory.Create(
            tournaments: new Dictionary<TournamentId, StageTournamentResults>
            {
                [qualifier.Id] = StageTournamentResultsFactory.Create(
                    qualifier.Id, TournamentStatus.Completed, qualifier.StartDate, qualifier.EndDate,
                    results: [.. bowlers.Take(9).Select((bowler, index) => StageResultRowFactory.Create(bowler.Id, index + 1))]),
                [survivor.Id] = StageTournamentResultsFactory.Create(
                    survivor.Id, TournamentStatus.Completed, survivor.StartDate, survivor.EndDate,
                    results: [.. bowlers.Select((bowler, index) => StageResultRowFactory.Create(bowler.Id, index + 1))],
                    squads:
                    [
                        StageSquadFactFactory.Create(
                            squadId, survivor.StartDate, NineAm,
                            [.. bowlers.Select((bowler, index) => SquadBowlerTotalFactory.Create(bowler.Id, 1500 - (index * 10), 6))]),
                    ]),
                [placement.Id] = StageTournamentResultsFactory.Create(
                    placement.Id, TournamentStatus.Completed, placement.StartDate, placement.EndDate,
                    results: [.. bowlers.Select((bowler, index) => StageResultRowFactory.Create(bowler.Id, index + 1))]),
            },
            membersOnDate: new Dictionary<DateOnly, IReadOnlySet<BowlerId>>
            {
                [qualifier.EndDate] = bowlers.Select(bowler => bowler.Id).ToHashSet(),
            },
            backfill: BackfillFactsFactory.Create(qualifier.EndDate, true, [BowlerPointsFactFactory.Create(bowlers[9].Id, 77)]));

        var qualifyingStage = series.Stages.Single(stage => stage.Kind == StageKind.Qualifying);
        var survivorStage = series.Stages.Single(stage => stage.Kind == StageKind.Survivor);
        var placementStage = series.Stages.Single(stage => stage.Kind == StageKind.Placement);

        _ = series.Advance(qualifier.Id, facts, Now);
        await _dbContext.SaveChangesAsync(ct);

        series.ClearSlot(qualifyingStage.Id, bowlers[2].Id, "Withdrew.", "01JTESTADMIN000000000000AA", series.Version, facts, Now)
            .IsError.ShouldBeFalse();
        await _dbContext.SaveChangesAsync(ct);

        series.AddOverride(qualifyingStage.Id, bowlers[8].Id, "Late entry.", "01JTESTADMIN000000000000BB", series.Version, facts, Now.AddHours(1))
            .IsError.ShouldBeFalse();
        await _dbContext.SaveChangesAsync(ct);

        return new Scenario(series, bowlers, qualifier, survivor, placement, qualifyingStage, survivorStage, placementStage, squadId);
    }

    [Fact(DisplayName = "ReadAsync should return null for an unknown Series")]
    public async Task ReadAsync_ShouldReturnNull_WhenSeriesDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        // Act
        var rows = await SeriesAdvancementReader.ReadAsync(_readContext, SeriesId.New(), ct);

        // Assert
        rows.ShouldBeNull();
    }

    [Fact(DisplayName = "ReadAsync should return the stored Version and BackfillAsOf in the header")]
    public async Task ReadAsync_ShouldReturnVersionAndBackfillAsOf()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await SeedAdvancedSeriesAsync();

        // Act
        var rows = await SeriesAdvancementReader.ReadAsync(_readContext, scenario.Series.Id, ct);

        // Assert
        rows.ShouldNotBeNull();
        rows.Header.Version.ShouldBe(scenario.Series.Version);
        rows.Header.Version.ShouldBeGreaterThan(0);
        rows.Header.BackfillAsOf.ShouldBe(scenario.Qualifier.EndDate);
    }

    [Fact(DisplayName = "ReadAsync should return Stages in order with the joined tournament fields and Stage state")]
    public async Task ReadAsync_ShouldReturnStagesInOrderWithTournamentFields()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await SeedAdvancedSeriesAsync();

        // Act
        var rows = await SeriesAdvancementReader.ReadAsync(_readContext, scenario.Series.Id, ct);

        // Assert
        rows.ShouldNotBeNull();
        rows.Stages.Select(stage => stage.Order).ShouldBe([1, 2, 3]);
        rows.Stages.Select(stage => stage.StageId).ShouldBe([scenario.QualifyingStage.Id, scenario.SurvivorStage.Id, scenario.PlacementStage.Id]);
        rows.Stages.Select(stage => stage.Kind).ShouldBe([StageKind.Qualifying, StageKind.Survivor, StageKind.Placement]);
        rows.Stages.Select(stage => stage.AdvancingCount).ShouldBe([8, 20, null]);
        rows.Stages.Select(stage => stage.TournamentId).ShouldBe([scenario.Qualifier.Id, scenario.Survivor.Id, scenario.Placement.Id]);
        rows.Stages.Select(stage => stage.TournamentName).ShouldBe(["The Qualifier", "The Survivor", "The Placement"]);
        rows.Stages.ShouldAllBe(stage => stage.TournamentType == TournamentFactory.ValidTournamentType);
        rows.Stages.ShouldAllBe(stage => stage.TournamentStatus == TournamentStatus.Completed);
        rows.Stages.ShouldAllBe(stage => stage.Status == StageStatus.Computed);
        rows.Stages.Select(stage => stage.ComputedAt).ShouldBe([Now.AddHours(1), Now.AddHours(1), Now.AddHours(1)]);
        rows.Stages.ShouldAllBe(stage => stage.OfficialAt == null);
        rows.Stages.ShouldAllBe(stage => !stage.IsStale);
        rows.Stages[0].SlotCount.ShouldBe(8);
    }

    [Fact(DisplayName = "ReadAsync should return each Stage's tournament type")]
    public async Task ReadAsync_ShouldReturnTheTournamentTypeOfEachStage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var qualifier = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "Doubles Qualifier", new DateOnly(2025, 2, 1), null, ct, type: TournamentType.Doubles);
        var survivor = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "Trios Survivor", new DateOnly(2025, 3, 1), null, ct, type: TournamentType.Trios);
        var placement = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "Baker Placement", new DateOnly(2025, 4, 1), null, ct, type: TournamentType.Baker);
        var series = await AdvancementReaderSeed.AddSeriesAsync(_dbContext, season, qualifier, survivor, placement, ct);

        // Act
        var rows = await SeriesAdvancementReader.ReadAsync(_readContext, series.Id, ct);

        // Assert
        rows.ShouldNotBeNull();
        rows.Stages.Select(stage => stage.TournamentType).ShouldBe([TournamentType.Doubles, TournamentType.Trios, TournamentType.Baker]);
        rows.Stages.Select(stage => stage.TournamentType.TeamSize).ShouldBe([2, 3, 5]);
    }

    [Fact(DisplayName = "ReadAsync should return the resolved squad designation with the squad-exists flag true when the squad has a row")]
    public async Task ReadAsync_ShouldFlagSquadExists_WhenResolvedSquadHasARow()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await SeedAdvancedSeriesAsync();

        // Act
        var rows = await SeriesAdvancementReader.ReadAsync(_readContext, scenario.Series.Id, ct);

        // Assert
        rows.ShouldNotBeNull();
        var qualifying = rows.Stages[0];
        qualifying.SquadDate.ShouldBeNull();
        qualifying.SquadStartTime.ShouldBeNull();
        qualifying.SquadId.ShouldBeNull();
        qualifying.SquadExists.ShouldBeFalse();
        var survivor = rows.Stages[1];
        survivor.SquadDate.ShouldBe(scenario.Survivor.StartDate);
        survivor.SquadStartTime.ShouldBe(NineAm);
        survivor.SquadId.ShouldBe(scenario.SquadId);
        survivor.SquadExists.ShouldBeTrue();
    }

    [Fact(DisplayName = "ReadAsync should report the squad as missing while keeping the designation when the designated squad has no row")]
    public async Task ReadAsync_ShouldFlagSquadMissing_WhenDesignatedSquadVanished()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var series = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct);
        var survivorStage = series.Stages.Single(stage => stage.Kind == StageKind.Survivor);
        var vanished = SquadId.New();
        var facts = AdvancementFactsFactory.Create(
            tournaments: new Dictionary<TournamentId, StageTournamentResults>
            {
                [survivorStage.TournamentId] = StageTournamentResultsFactory.Create(
                    survivorStage.TournamentId, TournamentStatus.Scheduled, squads: [StageSquadFactFactory.Create(vanished)]),
            });
        series.PickSquad(survivorStage.Id, vanished, series.Version, facts, Now).IsError.ShouldBeFalse();
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var rows = await SeriesAdvancementReader.ReadAsync(_readContext, series.Id, ct);

        // Assert
        rows.ShouldNotBeNull();
        var stage = rows.Stages.Single(row => row.Kind == StageKind.Survivor);
        stage.SquadId.ShouldBe(vanished);
        stage.SquadDate.ShouldBe(survivorStage.SquadDesignation!.Date);
        stage.SquadStartTime.ShouldBe(survivorStage.SquadDesignation.StartTime);
        stage.SquadExists.ShouldBeFalse();
    }

    [Fact(DisplayName = "ReadAsync should flag the squad as existing when the picked squad has a row")]
    public async Task ReadAsync_ShouldFlagSquadExists_WhenPickedSquadHasARow()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var series = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct);
        var survivorStage = series.Stages.Single(stage => stage.Kind == StageKind.Survivor);
        var tournament = _dbContext.Tournaments.Local.Single(t => t.Id == survivorStage.TournamentId);
        tournament.AddSquad(new DateTimeOffset(tournament.StartDate.Year, tournament.StartDate.Month, tournament.StartDate.Day, 14, 0, 0, TimeSpan.Zero))
            .IsError.ShouldBeFalse();
        await _dbContext.SaveChangesAsync(ct);
        var squadId = tournament.Squads.Single().Id;
        var facts = AdvancementFactsFactory.Create(
            tournaments: new Dictionary<TournamentId, StageTournamentResults>
            {
                [tournament.Id] = StageTournamentResultsFactory.Create(
                    tournament.Id, TournamentStatus.Scheduled, squads: [StageSquadFactFactory.Create(squadId)]),
            });
        series.PickSquad(survivorStage.Id, squadId, series.Version, facts, Now).IsError.ShouldBeFalse();
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var rows = await SeriesAdvancementReader.ReadAsync(_readContext, series.Id, ct);

        // Assert
        rows.ShouldNotBeNull();
        var stage = rows.Stages.Single(row => row.Kind == StageKind.Survivor);
        stage.SquadId.ShouldBe(squadId);
        stage.SquadExists.ShouldBeTrue();
    }

    [Fact(DisplayName = "ReadAsync should return a Pending Series with described squads, no output and Version zero")]
    public async Task ReadAsync_ShouldReturnPendingSeriesWithoutOutput()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var series = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct);

        // Act
        var rows = await SeriesAdvancementReader.ReadAsync(_readContext, series.Id, ct);

        // Assert
        rows.ShouldNotBeNull();
        rows.Header.Version.ShouldBe(0);
        rows.Header.BackfillAsOf.ShouldBeNull();
        rows.Stages.Count.ShouldBe(3);
        rows.Stages.ShouldAllBe(stage => stage.Status == StageStatus.Pending && stage.ComputedAt == null && stage.SlotCount == null);
        rows.Stages.Select(stage => stage.TournamentStatus).ShouldAllBe(status => status == TournamentStatus.Scheduled);
        var survivor = rows.Stages.Single(stage => stage.Kind == StageKind.Survivor);
        survivor.SquadDate.ShouldNotBeNull();
        survivor.SquadStartTime.ShouldBe(NineAm);
        survivor.SquadId.ShouldBeNull();
        survivor.SquadExists.ShouldBeFalse();
        rows.FieldEntries.ShouldBeEmpty();
        rows.SurvivorOutcomes.ShouldBeEmpty();
        rows.Positions.ShouldBeEmpty();
        rows.Overrides.ShouldBeEmpty();
    }

    [Fact(DisplayName = "ReadAsync should return field entries with bowler names and their source")]
    public async Task ReadAsync_ShouldReturnFieldEntriesWithNames()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await SeedAdvancedSeriesAsync();
        var bowlers = scenario.Bowlers;

        // Act
        var rows = await SeriesAdvancementReader.ReadAsync(_readContext, scenario.Series.Id, ct);

        // Assert
        rows.ShouldNotBeNull();
        rows.FieldEntries.Select(entry => entry.BowlerId).ShouldBe(
            [bowlers[0].Id, bowlers[1].Id, bowlers[3].Id, bowlers[4].Id, bowlers[5].Id, bowlers[6].Id, bowlers[7].Id, bowlers[8].Id, bowlers[9].Id],
            ignoreOrder: true);
        rows.FieldEntries.ShouldAllBe(entry => entry.BowlerName.LastName.StartsWith("Last", StringComparison.Ordinal));

        var qualified = rows.FieldEntries.Single(entry => entry.BowlerId == bowlers[0].Id);
        qualified.SourceStageId.ShouldBe(scenario.QualifyingStage.Id);
        qualified.Place.ShouldBe(1);
        qualified.BackfillPoints.ShouldBeNull();
        qualified.ViaOverride.ShouldBeFalse();
        qualified.BowlerName.ShouldBe(bowlers[0].Name);
        qualified.EntryId.ShouldBe(scenario.Series.FieldEntries.Single(entry => entry.BowlerId == bowlers[0].Id).Id);

        var added = rows.FieldEntries.Single(entry => entry.BowlerId == bowlers[8].Id);
        added.ViaOverride.ShouldBeTrue();
        added.SourceStageId.ShouldBe(scenario.QualifyingStage.Id);

        var backfilled = rows.FieldEntries.Single(entry => entry.BowlerId == bowlers[9].Id);
        backfilled.SourceStageId.ShouldBeNull();
        backfilled.BackfillPoints.ShouldBe(77);
        backfilled.ViaOverride.ShouldBeFalse();
        backfilled.BowlerName.ShouldBe(bowlers[9].Name);
    }

    [Fact(DisplayName = "ReadAsync should return Survivor outcomes with bowler names for the Stage that holds them")]
    public async Task ReadAsync_ShouldReturnSurvivorOutcomesWithNames()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await SeedAdvancedSeriesAsync();

        // Act
        var rows = await SeriesAdvancementReader.ReadAsync(_readContext, scenario.Series.Id, ct);

        // Assert
        rows.ShouldNotBeNull();
        rows.SurvivorOutcomes.Count.ShouldBe(9);
        rows.SurvivorOutcomes.ShouldAllBe(outcome => outcome.StageId == scenario.SurvivorStage.Id);
        foreach (var stored in scenario.SurvivorStage.SurvivorOutcomes)
        {
            var row = rows.SurvivorOutcomes.Single(outcome => outcome.BowlerId == stored.BowlerId);
            row.BlockTotal.ShouldBe(stored.BlockTotal);
            row.Advanced.ShouldBe(stored.Advanced);
            row.BowlerName.ShouldBe(scenario.Bowlers.Single(bowler => bowler.Id == stored.BowlerId).Name);
        }

        rows.SurvivorOutcomes.Single(outcome => outcome.BowlerId == scenario.Bowlers[0].Id).BlockTotal.ShouldBe(1500);
    }

    [Fact(DisplayName = "ReadAsync should return positions ordered by position with bowler names")]
    public async Task ReadAsync_ShouldReturnPositionsInOrderWithNames()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await SeedAdvancedSeriesAsync();

        // Act
        var rows = await SeriesAdvancementReader.ReadAsync(_readContext, scenario.Series.Id, ct);

        // Assert
        rows.ShouldNotBeNull();
        rows.Positions.Count.ShouldBe(9);
        rows.Positions.Select(position => position.Position).ShouldBe(scenario.Series.Positions.Select(position => position.Position).Order());
        foreach (var stored in scenario.Series.Positions)
        {
            var row = rows.Positions.Single(position => position.BowlerId == stored.BowlerId);
            row.Position.ShouldBe(stored.Position);
            row.Bowled.ShouldBe(stored.Bowled);
            row.IsFinalist.ShouldBe(stored.IsFinalist);
            row.BowlerName.ShouldBe(scenario.Bowlers.Single(bowler => bowler.Id == stored.BowlerId).Name);
        }
    }

    [Fact(DisplayName = "ReadAsync should return overrides with bowler names, kind, reason, author and whether they applied")]
    public async Task ReadAsync_ShouldReturnOverridesWithNames()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await SeedAdvancedSeriesAsync();
        var bowlers = scenario.Bowlers;

        // Act
        var rows = await SeriesAdvancementReader.ReadAsync(_readContext, scenario.Series.Id, ct);

        // Assert
        rows.ShouldNotBeNull();
        rows.Overrides.Count.ShouldBe(2);
        rows.Overrides.ShouldAllBe(row => row.StageId == scenario.QualifyingStage.Id && row.Applied);

        var clear = rows.Overrides.Single(row => row.Kind == OverrideKind.Clear);
        clear.BowlerId.ShouldBe(bowlers[2].Id);
        clear.BowlerName.ShouldBe(bowlers[2].Name);
        clear.Reason.ShouldBe("Withdrew.");
        clear.CreatedBy.ShouldBe("01JTESTADMIN000000000000AA");
        clear.CreatedAt.ShouldBe(Now);

        var add = rows.Overrides.Single(row => row.Kind == OverrideKind.Add);
        add.BowlerId.ShouldBe(bowlers[8].Id);
        add.BowlerName.ShouldBe(bowlers[8].Name);
        add.Reason.ShouldBe("Late entry.");
        add.CreatedBy.ShouldBe("01JTESTADMIN000000000000BB");
        add.CreatedAt.ShouldBe(Now.AddHours(1));
        add.OverrideId.ShouldBe(scenario.QualifyingStage.Overrides.Single(o => o.Kind == OverrideKind.Add).Id);
    }
}