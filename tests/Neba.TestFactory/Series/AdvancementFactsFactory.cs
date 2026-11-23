using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.TestFactory.Series;

public static class AdvancementFactsFactory
{
    /// <summary>Empty facts: no tournaments, no members, no Backfill, no names.</summary>
    public static AdvancementFacts Create(
        IReadOnlyDictionary<TournamentId, StageTournamentResults>? tournaments = null,
        IReadOnlyDictionary<DateOnly, IReadOnlySet<BowlerId>>? membersOnDate = null,
        BackfillFacts? backfill = null,
        IReadOnlyDictionary<BowlerId, BowlerSortName>? sortNames = null)
        => new()
        {
            Tournaments = tournaments ?? new Dictionary<TournamentId, StageTournamentResults>(),
            MembersOnDate = membersOnDate ?? new Dictionary<DateOnly, IReadOnlySet<BowlerId>>(),
            Backfill = backfill,
            SortNames = sortNames ?? new Dictionary<BowlerId, BowlerSortName>(),
        };

    /// <summary>Facts for the given tournaments, with every bowler in their results a member on the tournament end date.</summary>
    public static AdvancementFacts Bogus(int tournamentCount, int? seed = null)
    {
        var tournaments = StageTournamentResultsFactory.Bogus(tournamentCount, seed);

        return Create(
            tournaments: tournaments.ToDictionary(t => t.TournamentId),
            membersOnDate: tournaments
                .GroupBy(t => t.EndDate)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlySet<BowlerId>)g.SelectMany(t => t.Results).Select(r => r.BowlerId).ToHashSet()));
    }
}