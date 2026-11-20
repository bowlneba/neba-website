using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Series;
using Neba.TestFactory.Tournaments;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Features.Series;

/// <summary>
/// What a read scenario holds. Bowlers are referred to by index. Bowler ids ascend with the index, so a test knows the
/// BowlerId order without sorting.
/// </summary>
internal sealed record ReadScenario(
    SeriesAggregate Series,
    IReadOnlyList<Bowler> Bowlers,
    Tournament Qualifier,
    Tournament Survivor,
    Tournament Placement,
    SquadId SquadId,
    AdvancementFacts Facts)
{
    public SeriesStage Stage(int order)
        => Series.Stages.Single(stage => stage.Order == order);

    public BowlerId Id(int bowler)
        => Bowlers[bowler].Id;
}

/// <summary>How to build a read scenario. The defaults complete and compute all three Stages with nine bowlers qualifying.</summary>
internal sealed record ReadScenarioOptions
{
    public int BowlerCount { get; init; } = 12;

    /// <summary>One name per bowler. The default is First00 Last00, First01 Last01 and so on.</summary>
    public IReadOnlyList<Name>? Names { get; init; }

    /// <summary>The Qualifier results as (bowler index, place). The default places bowlers 0 to 8 first to ninth.</summary>
    public IReadOnlyList<(int Bowler, int Place)> QualifierResults { get; init; } = [.. Enumerable.Range(0, 9).Select(i => (i, i + 1))];

    /// <summary>Bowlers who are not members on the Qualifier's end date.</summary>
    public IReadOnlySet<int> NonMembers { get; init; } = new HashSet<int>();

    /// <summary>The open Bowler of the Year points the Backfill reads, as (bowler index, points). Null means not ready.</summary>
    public IReadOnlyList<(int Bowler, int Points)>? BackfillPoints { get; init; }

    /// <summary>The Survivor squad totals as (bowler index, total). The default gives every bowler a total.</summary>
    public IReadOnlyList<(int Bowler, int Total)>? SurvivorTotals { get; init; }

    /// <summary>How many Stages have a Completed tournament (1 to 3).</summary>
    public int CompletedStages { get; init; } = 3;

    /// <summary>Run the advancement for the Qualifier's tournament and save.</summary>
    public bool Advance { get; init; } = true;
}

internal static class AdvancementReadSeed
{
    public static readonly DateTimeOffset Now = new(2025, 8, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly TimeOnly NineAm = new(9, 0);

    public static async Task<ReadScenario> SeedAsync(AppDbContext dbContext, CancellationToken ct, ReadScenarioOptions? options = null)
    {
        options ??= new ReadScenarioOptions();

        var season = await SeriesSeed.AddSeasonAsync(dbContext, 2025, ct);
        var bowlers = await AddBowlersAsync(dbContext, options, ct);

        var qualifierResults = options.QualifierResults.Select(r => (Bowler: bowlers[r.Bowler], r.Place)).ToList();
        var survivorTotals = options.SurvivorTotals
            ?? [.. options.QualifierResults.Select(r => (r.Bowler, Total: 1500 - (r.Place * 10)))];
        var allBowlerRows = bowlers.Select((bowler, index) => (Bowler: bowler, Place: index + 1)).ToList();

        var qualifier = await AdvancementReaderSeed.AddTournamentAsync(
            dbContext, season, "The Qualifier", new DateOnly(2025, 2, 1), null, ct,
            status: TournamentStatus.Completed,
            results: [.. qualifierResults.Select(r => AdvancementReaderSeed.Result(r.Bowler, r.Place))]);

        var survivorDone = options.CompletedStages >= 2;
        var survivor = await AdvancementReaderSeed.AddTournamentAsync(
            dbContext, season, "The Survivor", new DateOnly(2025, 3, 3), null, ct,
            status: survivorDone ? TournamentStatus.Completed : null,
            results: survivorDone ? [.. allBowlerRows.Select(r => AdvancementReaderSeed.Result(r.Bowler, r.Place))] : null,
            squads: [SquadFactory.Create(new DateTimeOffset(2025, 3, 3, 14, 0, 0, TimeSpan.Zero))]);

        var placementDone = options.CompletedStages >= 3;
        var placement = await AdvancementReaderSeed.AddTournamentAsync(
            dbContext, season, "The Placement", new DateOnly(2025, 4, 5), null, ct,
            status: placementDone ? TournamentStatus.Completed : null,
            results: placementDone ? [.. allBowlerRows.Select(r => AdvancementReaderSeed.Result(r.Bowler, r.Place))] : null);

        var series = await AdvancementReaderSeed.AddSeriesAsync(dbContext, season, qualifier, survivor, placement, ct);
        var squadId = survivor.Squads.Single().Id;

        var facts = AdvancementFactsFactory.Create(
            tournaments: new Dictionary<TournamentId, StageTournamentResults>
            {
                [qualifier.Id] = StageTournamentResultsFactory.Create(
                    qualifier.Id, TournamentStatus.Completed, qualifier.StartDate, qualifier.EndDate,
                    results: [.. qualifierResults.Select(r => StageResultRowFactory.Create(r.Bowler.Id, r.Place))]),
                [survivor.Id] = StageTournamentResultsFactory.Create(
                    survivor.Id, survivorDone ? TournamentStatus.Completed : TournamentStatus.Scheduled, survivor.StartDate, survivor.EndDate,
                    results: survivorDone ? [.. allBowlerRows.Select(r => StageResultRowFactory.Create(r.Bowler.Id, r.Place))] : [],
                    squads:
                    [
                        StageSquadFactFactory.Create(
                            squadId, survivor.StartDate, NineAm,
                            [.. survivorTotals.Select(t => SquadBowlerTotalFactory.Create(bowlers[t.Bowler].Id, t.Total, 6))]),
                    ]),
                [placement.Id] = StageTournamentResultsFactory.Create(
                    placement.Id, placementDone ? TournamentStatus.Completed : TournamentStatus.Scheduled, placement.StartDate, placement.EndDate,
                    results: placementDone ? [.. allBowlerRows.Select(r => StageResultRowFactory.Create(r.Bowler.Id, r.Place))] : []),
            },
            membersOnDate: new Dictionary<DateOnly, IReadOnlySet<BowlerId>>
            {
                [qualifier.EndDate] = bowlers.Where((_, index) => !options.NonMembers.Contains(index)).Select(bowler => bowler.Id).ToHashSet(),
            },
            backfill: options.BackfillPoints is null
                ? null
                : BackfillFactsFactory.Create(
                    qualifier.EndDate, true,
                    [.. options.BackfillPoints.Select(p => BowlerPointsFactFactory.Create(bowlers[p.Bowler].Id, p.Points))]));

        if (options.Advance)
        {
            _ = series.Advance(qualifier.Id, facts, Now);
            await dbContext.SaveChangesAsync(ct);
        }

        return new ReadScenario(series, bowlers, qualifier, survivor, placement, squadId, facts);
    }

    private static async Task<IReadOnlyList<Bowler>> AddBowlersAsync(AppDbContext dbContext, ReadScenarioOptions options, CancellationToken ct)
    {
        var ids = Enumerable.Range(0, options.BowlerCount).Select(_ => BowlerId.New()).OrderBy(id => id.Value).ToList();

        var bowlers = ids
            .Select((id, index) => BowlerFactory.Create(
                id: id,
                name: options.Names?[index] ?? NameFactory.Create(firstName: $"First{index:00}", lastName: $"Last{index:00}")))
            .ToList();

        dbContext.Bowlers.AddRange(bowlers);
        await dbContext.SaveChangesAsync(ct);

        return bowlers;
    }
}