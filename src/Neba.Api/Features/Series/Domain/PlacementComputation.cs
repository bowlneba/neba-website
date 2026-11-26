using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Series.Domain;

internal static class PlacementComputation
{
    // Field bowlers who bowled rank by Place (ties by BowlerId), those with no real Place (below 1) last. Field bowlers
    // with no result row rank after them, alphabetically by last then first name (BowlerId last), and still get a position. Positions are unique. With a
    // Finale the first FinalistCount positions are finalists; without one IsFinalist is null.
    public static StageComputation Compute(
        IReadOnlyCollection<BowlerId> field,
        StageTournamentResults tournament,
        IReadOnlyDictionary<BowlerId, BowlerSortName> names,
        int? finalistCount)
    {
        var placeByBowler = tournament.Results.ToDictionary(row => row.BowlerId, row => row.Place);

        var bowled = field.Where(placeByBowler.ContainsKey)
            .OrderBy(bowler => placeByBowler[bowler] <= 0)
            .ThenBy(bowler => placeByBowler[bowler])
            .ThenBy(bowler => bowler);

        var absent = field.Where(bowler => !placeByBowler.ContainsKey(bowler))
            .OrderBy(bowler => names.TryGetValue(bowler, out var name) ? name.LastName : string.Empty, StringComparer.InvariantCultureIgnoreCase)
            .ThenBy(bowler => names.TryGetValue(bowler, out var name) ? name.FirstName : string.Empty, StringComparer.InvariantCultureIgnoreCase)
            .ThenBy(bowler => bowler);

        var ranked = bowled.Select(bowler => (Bowler: bowler, Bowled: true))
            .Concat(absent.Select(bowler => (Bowler: bowler, Bowled: false)))
            .Select((x, index) => new PositionDraft(
                x.Bowler, index + 1, x.Bowled, finalistCount is { } n ? index + 1 <= n : (bool?)null))
            .ToList();

        return new StageComputation { Positions = ranked };
    }
}