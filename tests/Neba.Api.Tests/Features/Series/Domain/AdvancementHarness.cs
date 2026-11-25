using ErrorOr;

using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Series;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Features.Series.Domain;

/// <summary>
/// A Pending Series with one Completed tournament per Stage, and the facts to advance it. Each Qualifying Stage has
/// <see cref="SlotCount"/> new bowlers, all members on the tournament end date and placed 1 to <see cref="SlotCount"/>. A
/// Survivor Stage has one squad (9:00 on the Stage date) with a total for every bowler in the field. Tests change the facts
/// between calls to model new results, a vanished squad or a Backfill that became ready.
/// </summary>
internal sealed class AdvancementHarness
{
    public const int SlotCount = 8;

    public static readonly DateTimeOffset Now = new(2025, 8, 1, 12, 0, 0, TimeSpan.Zero);
    public static readonly TimeSpan Grace = TimeSpan.FromHours(24);

    private readonly Dictionary<int, List<BowlerId>> _qualifiers;

    private AdvancementHarness(
        SeriesAggregate series, AdvancementFacts facts, Dictionary<int, List<BowlerId>> qualifiers, IReadOnlyList<BowlerId> field)
    {
        Series = series;
        Facts = facts;
        Field = field;
        _qualifiers = qualifiers;
    }

    public SeriesAggregate Series { get; }

    public AdvancementFacts Facts { get; private set; }

    /// <summary>Every Qualifying bowler, in Stage order.</summary>
    public IReadOnlyList<BowlerId> Field { get; }

    public static AdvancementHarness Create(params StageKind[] kinds)
    {
        var seasonId = SeasonId.New();
        var qualifiers = new Dictionary<int, List<BowlerId>>();
        var field = new List<BowlerId>();

        for (var index = 0; index < kinds.Length; index++)
        {
            var bowlers = kinds[index] == StageKind.Qualifying ? [.. AdvancementTestData.Ids(SlotCount)] : new List<BowlerId>();
            qualifiers[index + 1] = bowlers;
            field.AddRange(bowlers);
        }

        var definitions = new List<SeriesStageDefinition>();
        var stageFacts = new Dictionary<TournamentId, StageTournamentFacts>();
        var tournaments = new Dictionary<TournamentId, StageTournamentResults>();
        var members = new Dictionary<DateOnly, IReadOnlySet<BowlerId>>();
        var survivorsSeen = 0;

        for (var index = 0; index < kinds.Length; index++)
        {
            var kind = kinds[index];
            var tournamentId = TournamentId.New();
            var start = StageTournamentFactsFactory.ValidStartDate.AddDays(7 * index);
            var bowlers = kind == StageKind.Qualifying ? qualifiers[index + 1] : field;

            int? count = null;
            if (kind == StageKind.Qualifying)
            {
                count = SlotCount;
            }
            else if (kind == StageKind.Survivor)
            {
                count = 6 - (2 * survivorsSeen++);
            }

            definitions.Add(SeriesStageDefinitionFactory.Create(
                kind: kind,
                tournamentId: tournamentId,
                advancingCount: count,
                squadDate: kind == StageKind.Survivor ? start : null,
                squadStartTime: kind == StageKind.Survivor ? StageSquadFactFactory.ValidLocalStartTime : null));

            stageFacts[tournamentId] = StageTournamentFactsFactory.Create(tournamentId: tournamentId, seasonId: seasonId, startDate: start);

            tournaments[tournamentId] = StageTournamentResultsFactory.Create(
                tournamentId: tournamentId,
                status: TournamentStatus.Completed,
                startDate: start,
                results: [.. bowlers.Select((bowler, place) => StageResultRowFactory.Create(bowler, place + 1))],
                squads: kind == StageKind.Survivor ? [StageSquadFactFactory.Create(localDate: start, totals: Totals(bowlers))] : []);

            members[start] = bowlers.ToHashSet();
        }

        var series = SeriesFactory.Create(seasonId: seasonId, stages: definitions, tournaments: stageFacts);

        return new AdvancementHarness(series, AdvancementFactsFactory.Create(tournaments, members), qualifiers, field);
    }

    /// <summary>Totals that rank the bowlers in the order given: the first bowler scores highest.</summary>
    public static IReadOnlyList<SquadBowlerTotal> Totals(IEnumerable<BowlerId> bowlers)
        => [.. bowlers.Select((bowler, rank) => SquadBowlerTotalFactory.Create(bowler, 1500 - (rank * 10), 6))];

    public SeriesStage Stage(int order)
        => Series.Stages.Single(stage => stage.Order == order);

    public IReadOnlyList<BowlerId> Qualifiers(int order)
        => _qualifiers[order];

    public StageTournamentResults Tournament(int order)
        => Facts.Tournaments[Stage(order).TournamentId];

    public DateOnly EndDate(int order)
        => Tournament(order).EndDate;

    /// <summary>Advances from a Stage's tournament, the way a results event does.</summary>
    public AdvanceResult Advance(int order = 1, DateTimeOffset? now = null)
        => Series.Advance(Stage(order).TournamentId, Facts, now ?? Now);

    /// <summary>Advances and returns the harness, for set-up that does not need the result.</summary>
    public AdvancementHarness Advanced()
    {
        _ = Advance();

        return this;
    }

    public AdvanceResult MakeReadyOfficial(DateTimeOffset? now = null)
        => Series.MakeReadyStagesOfficial(now ?? Now + Grace);

    public ErrorOr<AdvanceResult> Recompute(int order, bool confirm = false, int? version = null, DateTimeOffset? now = null)
        => Series.Recompute(Stage(order).Id, confirm, version ?? Series.Version, Facts, now ?? Now);

    public ErrorOr<AdvanceResult> PickSquad(int order, SquadId squadId, int? version = null, DateTimeOffset? now = null)
        => Series.PickSquad(Stage(order).Id, squadId, version ?? Series.Version, Facts, now ?? Now);

    public ErrorOr<AdvanceResult> AddOverride(
        int order, BowlerId bowlerId, string reason = "Withdrew before the Stage.", int? version = null, DateTimeOffset? now = null)
        => Series.AddOverride(Stage(order).Id, bowlerId, reason, "admin-1", version ?? Series.Version, Facts, now ?? Now.AddHours(1));

    public ErrorOr<AdvanceResult> ClearSlot(
        int order, BowlerId bowlerId, string reason = "Withdrew before the Stage.", int? version = null, DateTimeOffset? now = null)
        => Series.ClearSlot(Stage(order).Id, bowlerId, reason, "admin-1", version ?? Series.Version, Facts, now ?? Now.AddHours(1));

    public ErrorOr<AdvanceResult> RemoveOverride(int order, StageOverrideId overrideId, int? version = null, DateTimeOffset? now = null)
        => Series.RemoveOverride(Stage(order).Id, overrideId, version ?? Series.Version, Facts, now ?? Now.AddHours(2));

    public AdvancementHarness Change(int order, Func<StageTournamentResults, StageTournamentResults> change)
    {
        var id = Stage(order).TournamentId;
        var tournaments = Facts.Tournaments.ToDictionary(pair => pair.Key, pair => pair.Value);
        tournaments[id] = change(tournaments[id]);
        Facts = Facts with { Tournaments = tournaments };

        return this;
    }

    public AdvancementHarness SetStatus(int order, TournamentStatus status)
        => Change(order, tournament => tournament with { Status = status });

    public AdvancementHarness SetResults(int order, IEnumerable<StageResultRow> rows)
        => Change(order, tournament => tournament with { Results = [.. rows] });

    public AdvancementHarness SetSquads(int order, IEnumerable<StageSquadFact> squads)
        => Change(order, tournament => tournament with { Squads = [.. squads] });

    /// <summary>Swaps the places of the first two Qualifying bowlers of a Stage, which changes its output.</summary>
    public AdvancementHarness SwapFirstTwoPlaces(int order)
    {
        var first = Qualifiers(order)[0];
        var second = Qualifiers(order)[1];

        return Change(order, tournament => tournament with
        {
            Results = [.. tournament.Results.Select(row => Swapped(row, first, second))],
        });
    }

    private static StageResultRow Swapped(StageResultRow row, BowlerId first, BowlerId second)
        => row.BowlerId switch
        {
            var id when id == first => row with { Place = 2 },
            var id when id == second => row with { Place = 1 },
            _ => row,
        };

    /// <summary>Replaces the members on a Stage's tournament end date.</summary>
    public AdvancementHarness SetMembers(int order, IEnumerable<BowlerId> members)
    {
        var onDate = Facts.MembersOnDate.ToDictionary(pair => pair.Key, pair => pair.Value);
        onDate[EndDate(order)] = members.ToHashSet();
        Facts = Facts with { MembersOnDate = onDate };

        return this;
    }

    public AdvancementHarness SetBackfill(BackfillFacts? backfill)
    {
        Facts = Facts with { Backfill = backfill };

        return this;
    }
}