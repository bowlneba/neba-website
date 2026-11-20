using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Series;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Features.Series;

/// <summary>A saved Series with a Qualifying, a Survivor and a Placement Stage, and twelve saved bowlers.</summary>
internal sealed record AdvancementScenario(
    SeriesAggregate Series,
    IReadOnlyList<BowlerId> Bowlers,
    Tournament Qualifier,
    Tournament Survivor,
    Tournament Placement,
    SquadId SquadId)
{
    public SeriesId SeriesId => Series.Id;

    public SeriesStage Stage(int order)
        => Series.Stages.Single(stage => stage.Order == order);
}

/// <summary>
/// The facts a run reads, as a test describes them. The defaults complete all three tournaments: bowlers 1 to 8 fill the
/// eight Qualifying slots (all members), nine bowlers bowl the Survivor squad and the Placement round, and every Stage has
/// the data it needs. The tournaments in the database stay Scheduled; the run reads these facts through a stub reader.
/// </summary>
internal sealed class AdvancementFactsScript(AdvancementScenario scenario)
{
    private static readonly TimeOnly NineAm = new(9, 0);

    public TournamentStatus QualifierStatus { get; set; } = TournamentStatus.Completed;

    public TournamentStatus SurvivorStatus { get; set; } = TournamentStatus.Completed;

    public TournamentStatus PlacementStatus { get; set; } = TournamentStatus.Completed;

    /// <summary>Bowlers in finishing order; place 1 first.</summary>
    public IReadOnlyList<BowlerId> QualifierPlaces { get; set; } = [.. scenario.Bowlers.Take(8)];

    /// <summary>Bowlers in the order of their Survivor totals; the highest total first.</summary>
    public IReadOnlyList<BowlerId> SurvivorRanking { get; set; } = [.. scenario.Bowlers.Take(9)];

    /// <summary>Bowlers in finishing order for the Placement round; place 1 first.</summary>
    public IReadOnlyList<BowlerId> PlacementPlaces { get; set; } = [.. scenario.Bowlers.Take(9)];

    public IReadOnlySet<BowlerId> Members { get; set; } = scenario.Bowlers.ToHashSet();

    public BackfillFacts? Backfill { get; set; }

    public AdvancementFacts Build()
        => AdvancementFactsFactory.Create(
            tournaments: new Dictionary<TournamentId, StageTournamentResults>
            {
                [scenario.Qualifier.Id] = StageTournamentResultsFactory.Create(
                    scenario.Qualifier.Id, QualifierStatus, scenario.Qualifier.StartDate, scenario.Qualifier.EndDate,
                    results: Rows(QualifierPlaces)),
                [scenario.Survivor.Id] = StageTournamentResultsFactory.Create(
                    scenario.Survivor.Id, SurvivorStatus, scenario.Survivor.StartDate, scenario.Survivor.EndDate,
                    results: Rows(SurvivorRanking),
                    squads:
                    [
                        StageSquadFactFactory.Create(
                            scenario.SquadId, scenario.Survivor.StartDate, NineAm,
                            [.. SurvivorRanking.Select((bowler, rank) => SquadBowlerTotalFactory.Create(bowler, 1500 - (rank * 10), 6))]),
                    ]),
                [scenario.Placement.Id] = StageTournamentResultsFactory.Create(
                    scenario.Placement.Id, PlacementStatus, scenario.Placement.StartDate, scenario.Placement.EndDate,
                    results: Rows(PlacementPlaces)),
            },
            membersOnDate: new Dictionary<DateOnly, IReadOnlySet<BowlerId>>
            {
                [scenario.Qualifier.EndDate] = Members,
            },
            backfill: Backfill);

    private static List<StageResultRow> Rows(IEnumerable<BowlerId> places)
        => [.. places.Select((bowler, index) => StageResultRowFactory.Create(bowler, index + 1))];
}

internal static class AdvancementRunSeed
{
    public static async Task<AdvancementScenario> SeedAsync(AppDbContext dbContext, CancellationToken ct)
    {
        var season = await SeriesSeed.AddSeasonAsync(dbContext, 2025, ct);
        var bowlers = await AdvancementReaderSeed.AddBowlersAsync(dbContext, 12, ct);

        var qualifier = await AdvancementReaderSeed.AddTournamentAsync(
            dbContext, season, "Run Qualifier", new DateOnly(2025, 2, 1), null, ct);
        var survivor = await AdvancementReaderSeed.AddTournamentAsync(
            dbContext, season, "Run Survivor", new DateOnly(2025, 3, 3), null, ct);
        var placement = await AdvancementReaderSeed.AddTournamentAsync(
            dbContext, season, "Run Placement", new DateOnly(2025, 4, 5), null, ct);

        var series = await AdvancementReaderSeed.AddSeriesAsync(dbContext, season, qualifier, survivor, placement, ct);

        var scenario = new AdvancementScenario(
            series, [.. bowlers.Select(bowler => bowler.Id)], qualifier, survivor, placement, SquadId.New());

        dbContext.ChangeTracker.Clear();

        return scenario;
    }
}