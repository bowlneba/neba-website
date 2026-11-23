using ErrorOr;

using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.GetSeriesAdvancement;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Series;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Features.Series.GetSeriesAdvancement;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class GetSeriesAdvancementQueryHandlerTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private const string Admin = "01JTESTADMIN000000000000AA";

    private static readonly DateTimeOffset Now = AdvancementReadSeed.Now;

    // Bowler indexes 0 to 11. Ids ascend with the index, so the BowlerId order is the index order.
    // Names are chosen so the name order differs from the id order inside each tie.
    private static readonly IReadOnlyList<Name> OrderingNames =
    [
        Name("Zed", "Zulu"),
        Name("Mia", "Zeta"),
        Name("Mia", "Alpha"),
        Name("Sam", "Same"),
        Name("Sam", "Same"),
        Name("Patricia", "Plain", "Pat"),
        Name("Rob", "Six"),
        Name("Rob", "Seven"),
        Name("Dan", "Zulu"),
        Name("Dan", "Alpha"),
        Name("Dan", "Alpha"),
        Name("Fay", "Last"),
    ];

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

    private static Name Name(string first, string last, string? nickname = null)
        => NameFactory.Create(firstName: first, lastName: last, nickname: nickname);

    private Task<ErrorOr<SeriesAdvancementDto>> ReadAsync(SeriesId seriesId)
        => new GetSeriesAdvancementQueryHandler(_readContext).HandleAsync(new GetSeriesAdvancementQuery { SeriesId = seriesId }, TestContext.Current.CancellationToken);

    // Slots 1 to 8 hold six members (bowlers 6 and 7 are not members), so two slots go vacant and the Backfill takes bowlers
    // 9, 8 and 10 (points 90, 50, 50: the tie at the cutoff all advance). Bowler 11 is outside the slots (place 9).
    // Six bowlers bowl the Survivor squad.
    private static ReadScenarioOptions OrderingOptions()
        => new()
        {
            Names = OrderingNames,
            QualifierResults = [(0, 1), (1, 2), (2, 2), (3, 4), (4, 4), (5, 6), (6, 7), (7, 8), (11, 9)],
            NonMembers = new HashSet<int> { 6, 7 },
            BackfillPoints = [(9, 90), (8, 50), (10, 50)],
            SurvivorTotals = [(0, 1500), (1, 1400), (2, 1400), (3, 1300), (4, 1300), (5, 1200)],
        };

    // The ordering scenario after an admin added bowler 11 to the Qualifying Stage.
    private async Task<ReadScenario> SeedOrderingScenarioAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var scenario = await AdvancementReadSeed.SeedAsync(_dbContext, ct, OrderingOptions());

        scenario.Series.AddOverride(
            scenario.Stage(1).Id, scenario.Id(11), "Late entry.", Admin, scenario.Series.Version, scenario.Facts, Now.AddHours(1))
            .IsError.ShouldBeFalse();
        await _dbContext.SaveChangesAsync(ct);

        return scenario;
    }

    [Fact(DisplayName = "HandleAsync should return NotFound when the Series does not exist")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenSeriesDoesNotExist()
    {
        // Arrange & Act
        var result = await ReadAsync(SeriesId.New());

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Series.NotFound");
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact(DisplayName = "HandleAsync should return empty lists, Version zero and the advancing count as the target field for a Series with all Stages Pending")]
    public async Task HandleAsync_ShouldReturnEmptyOutput_WhenAllStagesArePending()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var series = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct, name: "VH");

        // Act
        var result = await ReadAsync(series.Id);

        // Assert
        result.IsError.ShouldBeFalse();
        var dto = result.Value;
        dto.Version.ShouldBe(0);
        dto.BackfillAsOf.ShouldBeNull();
        dto.TargetField.ShouldBe(8);
        dto.Positions.ShouldBeEmpty();
        dto.Stages.Select(s => s.StageId).ShouldBe([.. series.Stages.OrderBy(s => s.Order).Select(s => s.Id)]);
        dto.Stages.Select(s => s.Order).ShouldBe([1, 2, 3]);
        dto.Stages.Select(s => s.Kind).ShouldBe(["Qualifying", "Survivor", "Placement"]);
        dto.Stages.ShouldAllBe(s => s.Status == "Pending" && s.TournamentStatus == "Scheduled");
        dto.Stages.ShouldAllBe(s => !s.IsStale && s.ComputedAt == null && !s.CanMakeOfficial && s.OfficialAt == null);
        dto.Stages.ShouldAllBe(s => s.SlotCount == null && s.VacantSlotCount == null && s.ResolvedSquadId == null);
        dto.Stages.ShouldAllBe(s => s.Entries.Count == 0 && s.Overrides.Count == 0 && s.SurvivorOutcomes.Count == 0);
        dto.Stages.Select(s => s.TournamentId).ShouldBe([.. series.Stages.OrderBy(s => s.Order).Select(s => s.TournamentId)]);
        dto.Stages.Select(s => s.NeedsSquad).ShouldBe([false, true, false]);
    }

    [Fact(DisplayName = "HandleAsync should size the target field as advancing count times team size for Pending Qualifying Stages")]
    public async Task HandleAsync_ShouldMultiplyAdvancingCountByTeamSize_WhenQualifyingStagesArePending()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var doubles = await SeriesSeed.AddTournamentAsync(_dbContext, season, "Doubles Qualifier", new DateOnly(2025, 2, 1), null, ct, TournamentType.Doubles);
        var trios = await SeriesSeed.AddTournamentAsync(_dbContext, season, "Trios Qualifier", new DateOnly(2025, 3, 1), null, ct, TournamentType.Trios);
        var series = SeriesFactory.CreateFor(
            season,
            [
                (doubles, StageKind.Qualifying, 4, null, null),
                (trios, StageKind.Qualifying, 5, null, null),
            ]);
        _dbContext.Series.Add(series);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var result = await ReadAsync(series.Id);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.TargetField.ShouldBe((4 * 2) + (5 * 3));
    }

    [Fact(DisplayName = "HandleAsync should size the target field from the SlotCount of a computed Qualifying Stage and the advancing count of a Pending one")]
    public async Task HandleAsync_ShouldUseSlotCount_WhenQualifyingStageIsComputed()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var bowlers = await AdvancementReaderSeed.AddBowlersAsync(_dbContext, 9, ct);
        var places = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 8 };
        var first = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "First Qualifier", new DateOnly(2025, 2, 1), null, ct,
            status: TournamentStatus.Completed,
            results: [.. bowlers.Select((bowler, index) => AdvancementReaderSeed.Result(bowler, places[index]))]);
        var second = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "Second Qualifier", new DateOnly(2025, 3, 1), null, ct, type: TournamentType.Doubles);
        var series = SeriesFactory.CreateFor(
            season,
            [
                (first, StageKind.Qualifying, 8, null, null),
                (second, StageKind.Qualifying, 5, null, null),
            ]);
        _dbContext.Series.Add(series);
        await _dbContext.SaveChangesAsync(ct);
        var facts = AdvancementFactsFactory.Create(
            tournaments: new Dictionary<TournamentId, StageTournamentResults>
            {
                [first.Id] = StageTournamentResultsFactory.Create(
                    first.Id, TournamentStatus.Completed, first.StartDate, first.EndDate,
                    results: [.. bowlers.Select((bowler, index) => StageResultRowFactory.Create(bowler.Id, places[index]))]),
            },
            membersOnDate: new Dictionary<DateOnly, IReadOnlySet<BowlerId>>
            {
                [first.EndDate] = bowlers.Select(bowler => bowler.Id).ToHashSet(),
            });
        _ = series.Advance(first.Id, facts, Now);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var result = await ReadAsync(series.Id);

        // Assert
        result.IsError.ShouldBeFalse();
        var stages = result.Value.Stages;
        stages[0].Status.ShouldBe("Computed");
        stages[0].SlotCount.ShouldBe(9);
        stages[1].Status.ShouldBe("Pending");
        stages[1].SlotCount.ShouldBeNull();
        result.Value.TargetField.ShouldBe(9 + (5 * 2));
    }

    [Fact(DisplayName = "HandleAsync should put the Backfill on the last Qualifying Stage only, with a null source Stage and the points")]
    public async Task HandleAsync_ShouldPutBackfillOnLastQualifyingStage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var bowlers = await AdvancementReaderSeed.AddBowlersAsync(_dbContext, 6, ct);
        var first = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "First Qualifier", new DateOnly(2025, 2, 1), null, ct,
            status: TournamentStatus.Completed,
            results: [AdvancementReaderSeed.Result(bowlers[0], 1), AdvancementReaderSeed.Result(bowlers[1], 2)]);
        var second = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "Second Qualifier", new DateOnly(2025, 3, 1), null, ct,
            status: TournamentStatus.Completed,
            results: [AdvancementReaderSeed.Result(bowlers[2], 1), AdvancementReaderSeed.Result(bowlers[3], 2)]);
        var series = SeriesFactory.CreateFor(
            season,
            [
                (first, StageKind.Qualifying, 2, null, null),
                (second, StageKind.Qualifying, 2, null, null),
            ]);
        _dbContext.Series.Add(series);
        await _dbContext.SaveChangesAsync(ct);

        // Bowler 1 is not a member on the first Qualifier's end date, so that slot goes vacant and the second
        // Qualifying Stage (the last) runs the Backfill for it.
        var facts = AdvancementFactsFactory.Create(
            tournaments: new Dictionary<TournamentId, StageTournamentResults>
            {
                [first.Id] = StageTournamentResultsFactory.Create(
                    first.Id, TournamentStatus.Completed, first.StartDate, first.EndDate,
                    results: [StageResultRowFactory.Create(bowlers[0].Id, 1), StageResultRowFactory.Create(bowlers[1].Id, 2)]),
                [second.Id] = StageTournamentResultsFactory.Create(
                    second.Id, TournamentStatus.Completed, second.StartDate, second.EndDate,
                    results: [StageResultRowFactory.Create(bowlers[2].Id, 1), StageResultRowFactory.Create(bowlers[3].Id, 2)]),
            },
            membersOnDate: new Dictionary<DateOnly, IReadOnlySet<BowlerId>>
            {
                [first.EndDate] = new[] { bowlers[0].Id, bowlers[2].Id, bowlers[3].Id, bowlers[4].Id }.ToHashSet(),
                [second.EndDate] = bowlers.Select(bowler => bowler.Id).ToHashSet(),
            },
            backfill: BackfillFactsFactory.Create(second.EndDate, true, [BowlerPointsFactFactory.Create(bowlers[4].Id, 60)]));
        _ = series.Advance(first.Id, facts, Now);
        await _dbContext.SaveChangesAsync(ct);
        var firstStage = series.Stages.Single(s => s.Order == 1);
        var secondStage = series.Stages.Single(s => s.Order == 2);

        // Act
        var result = await ReadAsync(series.Id);

        // Assert
        result.IsError.ShouldBeFalse();
        var dto = result.Value;
        dto.BackfillAsOf.ShouldBe(second.EndDate);
        dto.TargetField.ShouldBe(4);
        var firstDto = dto.Stages[0];
        firstDto.Entries.Select(e => e.BowlerId).ShouldBe([bowlers[0].Id]);
        firstDto.Entries.ShouldAllBe(e => e.SourceStageId == firstStage.Id);
        firstDto.VacantSlotCount.ShouldBe(1);
        var secondDto = dto.Stages[1];
        secondDto.Entries.Select(e => e.BowlerId).ShouldBe([bowlers[2].Id, bowlers[3].Id, bowlers[4].Id]);
        secondDto.Entries.Select(e => e.SourceStageId).ShouldBe([secondStage.Id, secondStage.Id, null]);
        secondDto.Entries.Select(e => e.Place).ShouldBe([1, 2, null]);
        secondDto.Entries.Select(e => e.BackfillPoints).ShouldBe([null, null, 60]);
        secondDto.VacantSlotCount.ShouldBe(0);
    }

    [Fact(DisplayName = "HandleAsync should order Qualifying entries by place with null last, then display name, then BowlerId, and map each field")]
    public async Task HandleAsync_ShouldOrderEntries_ByPlaceThenNameThenBowlerId()
    {
        // Arrange
        var scenario = await SeedOrderingScenarioAsync();
        var stageId = scenario.Stage(1).Id;

        // Act
        var result = await ReadAsync(scenario.Series.Id);

        // Assert
        result.IsError.ShouldBeFalse();
        var entries = result.Value.Stages[0].Entries;
        entries.Select(e => e.BowlerId).ShouldBe(
            [scenario.Id(0), scenario.Id(2), scenario.Id(1), scenario.Id(3), scenario.Id(4), scenario.Id(5), scenario.Id(11), scenario.Id(9), scenario.Id(10), scenario.Id(8)]);
        entries.Select(e => e.Place).ShouldBe([1, 2, 2, 4, 4, 6, 9, null, null, null]);
        entries.Select(e => e.SourceStageId).ShouldBe([stageId, stageId, stageId, stageId, stageId, stageId, stageId, null, null, null]);
        entries.Select(e => e.BackfillPoints).ShouldBe([null, null, null, null, null, null, null, 90, 50, 50]);
        entries.Select(e => e.ViaOverride).ShouldBe([false, false, false, false, false, false, true, false, false, false]);
        entries.Select(e => e.BowlerName).ShouldBe(
            ["Zed Zulu", "Mia Alpha", "Mia Zeta", "Sam Same", "Sam Same", "Pat Plain", "Fay Last", "Dan Alpha", "Dan Alpha", "Dan Zulu"]);
    }

    [Fact(DisplayName = "HandleAsync should count vacant slots without the entries an Add override put in, and leave the other Stages without entries")]
    public async Task HandleAsync_ShouldIgnoreOverrideEntries_WhenCountingVacantSlots()
    {
        // Arrange
        var scenario = await SeedOrderingScenarioAsync();

        // Act
        var result = await ReadAsync(scenario.Series.Id);

        // Assert
        result.IsError.ShouldBeFalse();
        var stages = result.Value.Stages;
        stages[0].SlotCount.ShouldBe(8);
        stages[0].VacantSlotCount.ShouldBe(2);
        stages[1].SlotCount.ShouldBeNull();
        stages[1].VacantSlotCount.ShouldBeNull();
        stages[2].VacantSlotCount.ShouldBeNull();
        stages[1].Entries.ShouldBeEmpty();
        stages[2].Entries.ShouldBeEmpty();
        result.Value.TargetField.ShouldBe(8);
    }

    [Fact(DisplayName = "HandleAsync should order Survivor outcomes by block total descending with null last, then display name, then BowlerId")]
    public async Task HandleAsync_ShouldOrderSurvivorOutcomes()
    {
        // Arrange
        var scenario = await SeedOrderingScenarioAsync();

        // Act
        var result = await ReadAsync(scenario.Series.Id);

        // Assert
        result.IsError.ShouldBeFalse();
        var outcomes = result.Value.Stages[1].SurvivorOutcomes;
        outcomes.Select(o => o.BowlerId).ShouldBe(
            [scenario.Id(0), scenario.Id(2), scenario.Id(1), scenario.Id(3), scenario.Id(4), scenario.Id(5), scenario.Id(9), scenario.Id(10), scenario.Id(8), scenario.Id(11)]);
        outcomes.Select(o => o.BlockTotal).ShouldBe([1500, 1400, 1400, 1300, 1300, 1200, null, null, null, null]);
        outcomes.Select(o => o.Advanced).ShouldBe([true, true, true, true, true, true, false, false, false, false]);
        outcomes.Select(o => o.BowlerName).ShouldBe(
            ["Zed Zulu", "Mia Alpha", "Mia Zeta", "Sam Same", "Sam Same", "Pat Plain", "Dan Alpha", "Dan Alpha", "Dan Zulu", "Fay Last"]);
    }

    [Fact(DisplayName = "HandleAsync should return the Placement positions in order with display names, the Version and the Backfill date")]
    public async Task HandleAsync_ShouldReturnPositionsVersionAndBackfillDate()
    {
        // Arrange
        var scenario = await SeedOrderingScenarioAsync();

        // Act
        var result = await ReadAsync(scenario.Series.Id);

        // Assert
        result.IsError.ShouldBeFalse();
        var dto = result.Value;
        dto.Version.ShouldBe(scenario.Series.Version);
        dto.Version.ShouldBeGreaterThan(0);
        dto.BackfillAsOf.ShouldBe(scenario.Qualifier.EndDate);
        dto.Positions.Select(p => p.Position).ShouldBe([1, 2, 3, 4, 5, 6]);
        dto.Positions.Select(p => p.BowlerId).ShouldBe([scenario.Id(0), scenario.Id(1), scenario.Id(2), scenario.Id(3), scenario.Id(4), scenario.Id(5)]);
        dto.Positions.Select(p => p.BowlerName).ShouldBe(["Zed Zulu", "Mia Zeta", "Mia Alpha", "Sam Same", "Sam Same", "Pat Plain"]);
        dto.Positions.ShouldAllBe(p => p.Bowled && p.IsFinalist == null);
    }

    [Fact(DisplayName = "HandleAsync should return kind, status and tournament status as names, with the resolved squad and no need for a squad")]
    public async Task HandleAsync_ShouldReturnNamesAndResolvedSquad_WhenAllStagesAreComputed()
    {
        // Arrange
        var scenario = await SeedOrderingScenarioAsync();

        // Act
        var result = await ReadAsync(scenario.Series.Id);

        // Assert
        result.IsError.ShouldBeFalse();
        var stages = result.Value.Stages;
        stages.Select(s => s.StageId).ShouldBe([scenario.Stage(1).Id, scenario.Stage(2).Id, scenario.Stage(3).Id]);
        stages.Select(s => s.Order).ShouldBe([1, 2, 3]);
        stages.Select(s => s.Kind).ShouldBe(["Qualifying", "Survivor", "Placement"]);
        stages.ShouldAllBe(s => s.Status == "Computed" && s.TournamentStatus == "Completed" && !s.IsStale && s.OfficialAt == null);
        stages.Select(s => s.TournamentId).ShouldBe([scenario.Qualifier.Id, scenario.Survivor.Id, scenario.Placement.Id]);
        stages.Select(s => s.ComputedAt).ShouldBe([Now.AddHours(1), Now.AddHours(1), Now]);
        stages.Select(s => s.ResolvedSquadId).ShouldBe([null, scenario.SquadId, null]);
        stages.ShouldAllBe(s => !s.NeedsSquad);
    }

    [Fact(DisplayName = "HandleAsync should return an applied Add override with its bowler name, reason, author and time")]
    public async Task HandleAsync_ShouldReturnOverrideFields()
    {
        // Arrange
        var scenario = await SeedOrderingScenarioAsync();
        var stored = scenario.Stage(1).Overrides.Single();

        // Act
        var result = await ReadAsync(scenario.Series.Id);

        // Assert
        result.IsError.ShouldBeFalse();
        var stages = result.Value.Stages;
        stages[1].Overrides.ShouldBeEmpty();
        stages[2].Overrides.ShouldBeEmpty();
        var mapped = stages[0].Overrides.Single();
        mapped.OverrideId.ShouldBe(stored.Id);
        mapped.BowlerId.ShouldBe(scenario.Id(11));
        mapped.BowlerName.ShouldBe("Fay Last");
        mapped.Kind.ShouldBe("Add");
        mapped.Reason.ShouldBe("Late entry.");
        mapped.CreatedBy.ShouldBe(Admin);
        mapped.CreatedAt.ShouldBe(Now.AddHours(1));
        mapped.Applied.ShouldBeTrue();
    }

    [Fact(DisplayName = "HandleAsync should order overrides by CreatedAt, then OverrideId, whatever order they were added in")]
    public async Task HandleAsync_ShouldOrderOverrides_ByCreatedAtThenOverrideId()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await AdvancementReadSeed.SeedAsync(_dbContext, ct, new ReadScenarioOptions
        {
            QualifierResults = [.. Enumerable.Range(0, 10).Select(i => (i, i + 1))],
        });
        var stageId = scenario.Stage(1).Id;
        var later = Now.AddHours(1);

        // Two Adds at the same instant, then a Clear at an earlier instant.
        scenario.Series.AddOverride(stageId, scenario.Id(8), "Late one.", Admin, scenario.Series.Version, scenario.Facts, later).IsError.ShouldBeFalse();
        scenario.Series.AddOverride(stageId, scenario.Id(9), "Late two.", Admin, scenario.Series.Version, scenario.Facts, later).IsError.ShouldBeFalse();
        scenario.Series.ClearSlot(stageId, scenario.Id(0), "Withdrew.", Admin, scenario.Series.Version, scenario.Facts, Now).IsError.ShouldBeFalse();
        await _dbContext.SaveChangesAsync(ct);
        var sameInstant = scenario.Stage(1).Overrides.Where(o => o.CreatedAt == later).OrderBy(o => o.Id.Value).Select(o => o.Id).ToList();

        // Act
        var result = await ReadAsync(scenario.Series.Id);

        // Assert
        result.IsError.ShouldBeFalse();
        var overrides = result.Value.Stages[0].Overrides;
        overrides.Count.ShouldBe(3);
        overrides[0].Kind.ShouldBe("Clear");
        overrides[0].BowlerId.ShouldBe(scenario.Id(0));
        overrides[0].CreatedAt.ShouldBe(Now);
        overrides.Skip(1).Select(o => o.OverrideId).ShouldBe(sameInstant);
        overrides.Skip(1).ShouldAllBe(o => o.Kind == "Add" && o.CreatedAt == later);
    }

    [Fact(DisplayName = "HandleAsync should let only the first Computed Stage be made Official, and never the Placement Stage")]
    public async Task HandleAsync_ShouldSetCanMakeOfficial_ForTheFirstComputedStageOnly()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await AdvancementReadSeed.SeedAsync(_dbContext, ct);

        // Act
        var result = await ReadAsync(scenario.Series.Id);

        // Assert
        result.IsError.ShouldBeFalse();
        var stages = result.Value.Stages;
        stages.ShouldAllBe(s => s.Status == "Computed" && s.ComputedAt == Now);
        stages.Select(s => s.CanMakeOfficial).ShouldBe([true, false, false]);
    }

    [Fact(DisplayName = "HandleAsync should let the next Stage be made Official once the earlier Stage is Official")]
    public async Task HandleAsync_ShouldSetCanMakeOfficial_ForNextStage_WhenEarlierStageIsOfficial()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await AdvancementReadSeed.SeedAsync(_dbContext, ct);
        scenario.Stage(1).MarkOfficial(Now.AddHours(2));
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var result = await ReadAsync(scenario.Series.Id);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Stages.Select(s => s.CanMakeOfficial).ShouldBe([false, true, false]);
    }

    [Fact(DisplayName = "HandleAsync should not let a stale Stage be made Official")]
    public async Task HandleAsync_ShouldNotSetCanMakeOfficial_WhenStageIsStale()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await AdvancementReadSeed.SeedAsync(_dbContext, ct);
        scenario.Stage(1).MarkStale();
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var result = await ReadAsync(scenario.Series.Id);

        // Assert
        result.IsError.ShouldBeFalse();
        var stages = result.Value.Stages;
        stages[0].IsStale.ShouldBeTrue();
        stages[0].CanMakeOfficial.ShouldBeFalse();
        stages[1].CanMakeOfficial.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync should return the OfficialAt and no CanMakeOfficial for an Official Stage")]
    public async Task HandleAsync_ShouldNotSetCanMakeOfficial_WhenStageIsOfficial()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await AdvancementReadSeed.SeedAsync(_dbContext, ct);
        scenario.Stage(1).MarkOfficial(Now.AddHours(2));
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var result = await ReadAsync(scenario.Series.Id);

        // Assert
        result.IsError.ShouldBeFalse();
        var official = result.Value.Stages[0];
        official.Status.ShouldBe("Official");
        official.OfficialAt.ShouldBe(Now.AddHours(2));
        official.CanMakeOfficial.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync should not let the last Qualifying Stage be made Official while it awaits the Backfill")]
    public async Task HandleAsync_ShouldNotSetCanMakeOfficial_WhenLastQualifyingStageAwaitsBackfill()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await AdvancementReadSeed.SeedAsync(_dbContext, ct, new ReadScenarioOptions
        {
            NonMembers = new HashSet<int> { 2 },
            BackfillPoints = null,
        });

        // Act
        var result = await ReadAsync(scenario.Series.Id);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.BackfillAsOf.ShouldBeNull();
        var qualifying = result.Value.Stages[0];
        qualifying.Status.ShouldBe("Computed");
        qualifying.IsStale.ShouldBeFalse();
        qualifying.ComputedAt.ShouldBe(Now);
        qualifying.VacantSlotCount.ShouldBe(1);
        qualifying.CanMakeOfficial.ShouldBeFalse();
        result.Value.Stages.Skip(1).ShouldAllBe(s => s.Status == "Pending");
    }

    [Fact(DisplayName = "HandleAsync should return empty positions while the Placement Stage has not computed")]
    public async Task HandleAsync_ShouldReturnNoPositions_WhenPlacementStageIsPending()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await AdvancementReadSeed.SeedAsync(_dbContext, ct, new ReadScenarioOptions { CompletedStages = 2 });

        // Act
        var result = await ReadAsync(scenario.Series.Id);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Stages.Select(s => s.Status).ShouldBe(["Computed", "Computed", "Pending"]);
        result.Value.Stages[2].TournamentStatus.ShouldBe("Scheduled");
        result.Value.Positions.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should flag NeedsSquad for a Survivor Stage whose designated squad no longer exists, and keep the designated squad id")]
    public async Task HandleAsync_ShouldFlagNeedsSquad_WhenDesignatedSquadVanished()
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
        var result = await ReadAsync(series.Id);

        // Assert
        result.IsError.ShouldBeFalse();
        var survivor = result.Value.Stages.Single(s => s.Kind == "Survivor");
        survivor.NeedsSquad.ShouldBeTrue();
        survivor.ResolvedSquadId.ShouldBe(vanished);
        result.Value.Stages.Where(s => s.Kind != "Survivor").ShouldAllBe(s => !s.NeedsSquad);
    }
}