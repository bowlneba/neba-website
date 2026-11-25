using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.GetSeriesAdvancers;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Series;
using Neba.TestFactory.Tournaments;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Features.Series.GetSeriesAdvancers;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class GetSeriesAdvancersQueryHandlerTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = AdvancementReadSeed.Now;

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

    private async Task<SeriesAdvancersDto> ReadAsync(SeriesId seriesId)
    {
        var result = await new GetSeriesAdvancersQueryHandler(_readContext)
            .HandleAsync(new GetSeriesAdvancersQuery { SeriesId = seriesId }, TestContext.Current.CancellationToken);
        result.IsError.ShouldBeFalse();
        return result.Value;
    }

    [Fact(DisplayName = "HandleAsync should return NotFound for an unknown Series")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenSeriesDoesNotExist()
    {
        // Arrange & Act
        var result = await new GetSeriesAdvancersQueryHandler(_readContext)
            .HandleAsync(new GetSeriesAdvancersQuery { SeriesId = SeriesId.New() }, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Series.NotFound");
    }

    [Fact(DisplayName = "HandleAsync should return no Stages when every Qualifying and Survivor Stage is only Computed")]
    public async Task HandleAsync_ShouldReturnNoStages_WhenNothingIsOfficial()
    {
        // Arrange
        var scenario = await AdvancementReadSeed.SeedAsync(_dbContext, TestContext.Current.CancellationToken);

        // Act
        var dto = await ReadAsync(scenario.Series.Id);

        // Assert
        dto.Stages.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should list the names of an Official Qualifying Stage by place, with no admin data")]
    public async Task HandleAsync_ShouldListQualifiers_WhenQualifyingStageIsOfficial()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await AdvancementReadSeed.SeedAsync(_dbContext, ct);
        scenario.Stage(1).MarkOfficial(Now.AddHours(2));
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var dto = await ReadAsync(scenario.Series.Id);

        // Assert
        var stage = dto.Stages.Single();
        stage.Order.ShouldBe(1);
        stage.Kind.ShouldBe("Qualifying");
        stage.Advancers.Select(a => a.BowlerId).Take(3).ShouldBe([scenario.Id(0), scenario.Id(1), scenario.Id(2)]);
        stage.Advancers.Select(a => a.Place).Take(3).ShouldBe([1, 2, 3]);
        stage.Advancers.ShouldAllBe(a => a.BlockTotal == null);
    }

    [Fact(DisplayName = "HandleAsync should include Backfill entries, flagged and last, in the last Qualifying Stage")]
    public async Task HandleAsync_ShouldFlagBackfillEntries_InLastQualifyingStage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await AdvancementReadSeed.SeedAsync(_dbContext, ct, new ReadScenarioOptions
        {
            QualifierResults = [(0, 1), (1, 2), (2, 3), (3, 4), (4, 5), (5, 6), (6, 7), (7, 8)],
            NonMembers = new HashSet<int> { 6, 7 },
            BackfillPoints = [(9, 90), (8, 50)],
        });
        scenario.Stage(1).MarkOfficial(Now.AddHours(2));
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var dto = await ReadAsync(scenario.Series.Id);

        // Assert
        var advancers = dto.Stages.Single().Advancers;
        advancers.Where(a => a.IsBackfill).Select(a => a.BowlerId).ShouldBe([scenario.Id(8), scenario.Id(9)], ignoreOrder: true);
        advancers.Take(advancers.Count(a => !a.IsBackfill)).ShouldAllBe(a => !a.IsBackfill && a.Place != null);
        advancers.Where(a => a.IsBackfill).ShouldAllBe(a => a.Place == null);
    }

    [Fact(DisplayName = "HandleAsync should sort Backfill entries by Bowler of the Year points, high to low, and return the points")]
    public async Task HandleAsync_ShouldSortBackfillEntries_ByPointsDescending()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await AdvancementReadSeed.SeedAsync(_dbContext, ct, new ReadScenarioOptions
        {
            QualifierResults = [(0, 1), (1, 2), (2, 3), (3, 4), (4, 5), (5, 6), (6, 7), (7, 8)],
            NonMembers = new HashSet<int> { 6, 7 },
            BackfillPoints = [(8, 50), (9, 90)],
        });
        scenario.Stage(1).MarkOfficial(Now.AddHours(2));
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var dto = await ReadAsync(scenario.Series.Id);

        // Assert
        var backfill = dto.Stages.Single().Advancers.Where(a => a.IsBackfill).ToList();
        backfill.Select(a => a.BowlerId).ShouldBe([scenario.Id(9), scenario.Id(8)]);
        backfill.Select(a => a.BackfillPoints).ShouldBe([90, 50]);
    }

    [Fact(DisplayName = "HandleAsync should list the full standings of an Official Survivor Stage, best total first, flagging who advanced")]
    public async Task HandleAsync_ShouldListFullStandings_WhenSurvivorStageIsOfficial()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await AdvancementReadSeed.SeedAsync(_dbContext, ct);
        scenario.Stage(1).MarkOfficial(Now.AddHours(2));
        scenario.Stage(2).MarkOfficial(Now.AddHours(3));
        await _dbContext.SaveChangesAsync(ct);
        var outcomes = scenario.Stage(2).SurvivorOutcomes;

        // Act
        var dto = await ReadAsync(scenario.Series.Id);

        // Assert
        var survivor = dto.Stages.Single(s => s.Kind == "Survivor");
        survivor.Order.ShouldBe(2);
        survivor.Advancers.Count.ShouldBe(outcomes.Count);
        survivor.Advancers.Count(a => a.Advanced).ShouldBe(outcomes.Count(o => o.Advanced));
        survivor.Advancers.ShouldAllBe(a => a.Place == null && !a.IsBackfill && a.AlreadyAdvancedFromOrder == null);
        survivor.Advancers.Where(a => a.BlockTotal != null).Select(a => a.BlockTotal!.Value)
            .ShouldBe(survivor.Advancers.Where(a => a.BlockTotal != null).Select(a => a.BlockTotal!.Value).OrderDescending());
    }

    [Fact(DisplayName = "HandleAsync should place a bowler who already advanced in order, marked with the earlier Stage")]
    public async Task HandleAsync_ShouldMarkAlreadyAdvancedBowler_InPlace()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var bowlers = await AdvancementReaderSeed.AddBowlersAsync(_dbContext, 4, ct);
        var first = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "First Qualifier", new DateOnly(2025, 2, 1), null, ct,
            status: TournamentStatus.Completed,
            results: [AdvancementReaderSeed.Result(bowlers[0], 1), AdvancementReaderSeed.Result(bowlers[1], 2), AdvancementReaderSeed.Result(bowlers[3], 3)]);
        var second = await AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, "Second Qualifier", new DateOnly(2025, 3, 1), null, ct,
            status: TournamentStatus.Completed,
            results: [AdvancementReaderSeed.Result(bowlers[1], 1), AdvancementReaderSeed.Result(bowlers[2], 2), AdvancementReaderSeed.Result(bowlers[3], 3)]);
        var series = SeriesFactory.CreateFor(
            season,
            [
                (first, StageKind.Qualifying, 2, null, null),
                (second, StageKind.Qualifying, 2, null, null),
            ]);
        _dbContext.Series.Add(series);
        await _dbContext.SaveChangesAsync(ct);

        var facts = AdvancementFactsFactory.Create(
            tournaments: new Dictionary<TournamentId, StageTournamentResults>
            {
                [first.Id] = StageTournamentResultsFactory.Create(
                    first.Id, TournamentStatus.Completed, first.StartDate, first.EndDate,
                    results: [StageResultRowFactory.Create(bowlers[0].Id, 1), StageResultRowFactory.Create(bowlers[1].Id, 2), StageResultRowFactory.Create(bowlers[3].Id, 3)]),
                [second.Id] = StageTournamentResultsFactory.Create(
                    second.Id, TournamentStatus.Completed, second.StartDate, second.EndDate,
                    results: [StageResultRowFactory.Create(bowlers[1].Id, 1), StageResultRowFactory.Create(bowlers[2].Id, 2), StageResultRowFactory.Create(bowlers[3].Id, 3)]),
            },
            membersOnDate: new Dictionary<DateOnly, IReadOnlySet<BowlerId>>
            {
                [first.EndDate] = bowlers.Select(bowler => bowler.Id).ToHashSet(),
                [second.EndDate] = bowlers.Select(bowler => bowler.Id).ToHashSet(),
            },
            backfill: BackfillFactsFactory.Create(second.EndDate, true, []));
        _ = series.Advance(first.Id, facts, Now);
        foreach (var stage in series.Stages)
        {
            stage.MarkOfficial(Now.AddHours(2));
        }

        await _dbContext.SaveChangesAsync(ct);

        // Act
        var dto = await ReadAsync(series.Id);

        // Assert
        var stageTwo = dto.Stages.Single(s => s.Order == 2);
        var kyle = stageTwo.Advancers.Single(a => a.BowlerId == bowlers[1].Id);
        kyle.Advanced.ShouldBeFalse();
        kyle.AlreadyAdvancedFromOrder.ShouldBe(1);
        kyle.Place.ShouldBe(1);
        stageTwo.Advancers[0].ShouldBe(kyle);
        stageTwo.Advancers.Where(a => a.BowlerId != bowlers[1].Id).ShouldAllBe(a => a.Advanced && a.AlreadyAdvancedFromOrder == null);
    }

    [Fact(DisplayName = "HandleAsync should leave out a Computed Survivor Stage even when an earlier Stage is Official")]
    public async Task HandleAsync_ShouldLeaveOutComputedSurvivorStage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await AdvancementReadSeed.SeedAsync(_dbContext, ct);
        scenario.Stage(1).MarkOfficial(Now.AddHours(2));
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var dto = await ReadAsync(scenario.Series.Id);

        // Assert
        dto.Stages.Select(s => s.Kind).ShouldBe(["Qualifying"]);
    }

    [Fact(DisplayName = "HandleAsync should list every position of an Official Placement Stage, none marked a finalist, when the Series has no Finale")]
    public async Task HandleAsync_ShouldListEveryPosition_WhenNoFinale()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await AdvancementReadSeed.SeedAsync(_dbContext, ct);
        MarkAllOfficial(scenario.Series);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var dto = await ReadAsync(scenario.Series.Id);

        // Assert
        var placement = dto.Stages.Single(s => s.Kind == "Placement");
        placement.Advancers.ShouldNotBeEmpty();
        placement.Advancers.ShouldAllBe(a => a.IsFinalist == null && a.Position != null);
        placement.Advancers.Select(a => a.Position!.Value).ShouldBe(placement.Advancers.Select(a => a.Position!.Value).Order());
    }

    [Fact(DisplayName = "HandleAsync should leave out a Computed Placement Stage even when the Series has a Finale")]
    public async Task HandleAsync_ShouldLeaveOutComputedPlacementStage_WhenSeriesHasFinale()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await AdvancementReadSeed.SeedAsync(_dbContext, ct, new ReadScenarioOptions
        {
            Finale = SeriesFinaleDefinitionFactory.Create(finalistCount: 3),
        });
        scenario.Stage(1).MarkOfficial(Now.AddHours(2));
        scenario.Stage(2).MarkOfficial(Now.AddHours(3));
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var dto = await ReadAsync(scenario.Series.Id);

        // Assert
        dto.Stages.ShouldNotContain(s => s.Kind == "Placement");
    }

    [Fact(DisplayName = "HandleAsync should list every position of an Official Placement Stage and mark the finalists when the Series has a Finale")]
    public async Task HandleAsync_ShouldMarkFinalists_WhenPlacementStageIsOfficial()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await AdvancementReadSeed.SeedAsync(_dbContext, ct, new ReadScenarioOptions
        {
            Finale = SeriesFinaleDefinitionFactory.Create(finalistCount: 3),
        });
        MarkAllOfficial(scenario.Series);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var dto = await ReadAsync(scenario.Series.Id);

        // Assert
        var placement = dto.Stages.Single(s => s.Kind == "Placement");
        placement.Advancers.Count(a => a.IsFinalist == true).ShouldBe(3);
        placement.Advancers.ShouldAllBe(a => !a.IsBackfill && a.BlockTotal == null && a.IsFinalist != null && a.Advanced == a.IsFinalist);
        placement.Advancers.Take(3).ShouldAllBe(a => a.IsFinalist == true);
    }

    private static void MarkAllOfficial(SeriesAggregate series)
    {
        foreach (var stage in series.Stages)
        {
            stage.MarkOfficial(Now.AddHours(2));
        }
    }
}