namespace Neba.Api.Legacy.Tournaments.Complete;

// Pure mapping logic - no I/O - so it's unit-testable on its own, in isolation from
// SyncTournamentResultsJob's Dapper/EF plumbing. Returns a Place for every bowler that can be
// placed: those with a real Place already, plus (singles) individually-ranked fills, plus (team)
// roster-ranked fills, plus every remaining bowler whose only roster(s) were forfeited, sharing
// one shared last-place value.
//
// Singles: a bowler's multiple qualifying entries (re-entries) reduce to their single best entry
// (max Score, tiebreak max HighGame) before ranking - same rule nebamgmt-v3 itself uses for cuts
// and seeding (GetBowlersTopScore.GetTopScore), with a deterministic tiebreak in place of its
// arbitrary .First()-on-tie.
//
// Team: a roster's composite score for one squad is the sum of its members' scores *for that
// same squad* - never mixed across a roster's other squad-entries. A roster with more than one
// squad-entry reduces to its single best composite entry, the same "best entry wins" rule as
// singles but scored at the roster level. Forfeited rosters (Teams.Forfeit) are excluded from
// ranking entirely. A bowler is placed through at most one non-forfeited roster (guaranteed by
// tournament rules); a bowler with no non-forfeited roster at all shares one common last place
// with every other such bowler, untied to any score.
internal static class TournamentPlaceCalculator
{
    public static Dictionary<int, int> ComputePlaces(
        IReadOnlyCollection<LegacyResultRow> results,
        IReadOnlyCollection<LegacyQualifyingRow> qualifying,
        IReadOnlyCollection<LegacyTeamRow> teams,
        IReadOnlyCollection<LegacyTeamMemberRow> teamMembers,
        IReadOnlyCollection<LegacyTeamSquadRow> teamSquads)
    {
        var places = results
            .Where(r => r.Place.HasValue)
            .ToDictionary(r => r.BowlerId, r => r.Place!.Value);

        var nextPlace = (places.Count > 0 ? places.Values.Max() : 0) + 1;

        var missingBowlerIds = results
            .Where(r => !r.Place.HasValue)
            .Select(r => r.BowlerId)
            .ToHashSet();

        var isTeamTournament = teams.Count > 0;

        return isTeamTournament
            ? ComputeTeamPlaces(places, nextPlace, missingBowlerIds, qualifying, teams, teamMembers, teamSquads)
            : ComputeSinglesPlaces(places, nextPlace, missingBowlerIds, qualifying);
    }

    private static Dictionary<int, int> ComputeSinglesPlaces(
        Dictionary<int, int> places,
        int nextPlace,
        HashSet<int> missingBowlerIds,
        IReadOnlyCollection<LegacyQualifyingRow> qualifying)
    {
        // A bowler can have more than one qualifying row (re-entries) - collapse to their single
        // best entry first.
        var bestQualifyingByBowlerId = qualifying
            .GroupBy(q => q.BowlerId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(q => q.Score).ThenByDescending(q => q.HighGame).First());

        foreach (var bowlerId in missingBowlerIds
            .Where(bestQualifyingByBowlerId.ContainsKey)
            .OrderByDescending(id => bestQualifyingByBowlerId[id].Games)
            .ThenByDescending(id => bestQualifyingByBowlerId[id].Score)
            .ThenByDescending(id => bestQualifyingByBowlerId[id].HighGame))
        {
            places[bowlerId] = nextPlace++;
        }

        return places;
    }

    private static Dictionary<int, int> ComputeTeamPlaces(
        Dictionary<int, int> places,
        int nextPlace,
        HashSet<int> missingBowlerIds,
        IReadOnlyCollection<LegacyQualifyingRow> qualifying,
        IReadOnlyCollection<LegacyTeamRow> teams,
        IReadOnlyCollection<LegacyTeamMemberRow> teamMembers,
        IReadOnlyCollection<LegacyTeamSquadRow> teamSquads)
    {
        // A roster's composite score for one squad is the sum of its members' scores *for that
        // specific squad* - never mixed across a roster's other squad-entries.
        var qualifyingByBowlerAndSquad = qualifying.ToDictionary(q => (q.BowlerId, q.SquadId));
        var membersByTeamId = teamMembers
            .GroupBy(m => m.TeamId)
            .ToDictionary(g => g.Key, g => g.Select(m => m.BowlerId).ToList());
        var forfeitByTeamId = teams.ToDictionary(t => t.TeamId, t => t.Forfeit);

        // Reduce each roster to its single best (roster, squad) composite entry - the winning
        // entry's own HighGame travels with it rather than being maxed independently.
        var bestEntryByTeamId = teamSquads
            .GroupBy(ts => ts.TeamId)
            .ToDictionary(g => g.Key, g =>
            {
                var members = membersByTeamId.GetValueOrDefault(g.Key, []);
                return g.Select(ts => new
                {
                    ts.HighGame,
                    Score = members.Sum(b => qualifyingByBowlerAndSquad.TryGetValue((b, ts.SquadId), out var q) ? q.Score : 0),
                    Games = members.Sum(b => qualifyingByBowlerAndSquad.TryGetValue((b, ts.SquadId), out var q) ? q.Games : 0)
                })
                .OrderByDescending(entry => entry.Score)
                .ThenByDescending(entry => entry.HighGame)
                .First();
            });

        // Bowler -> the one non-forfeited roster they belong to, if any. Guaranteed at most one
        // per tournament rules - if that's ever violated in real data, the ToDictionary below
        // throws, surfacing it as a data anomaly rather than silently picking one.
        var countingTeamIdByBowlerId = teamMembers
            .Where(m => !forfeitByTeamId.GetValueOrDefault(m.TeamId))
            .ToDictionary(m => m.BowlerId, m => m.TeamId);

        var rankedTeamIds = missingBowlerIds
            .Where(countingTeamIdByBowlerId.ContainsKey)
            .Select(id => countingTeamIdByBowlerId[id])
            .Distinct()
            .Where(bestEntryByTeamId.ContainsKey)
            .OrderByDescending(teamId => bestEntryByTeamId[teamId].Games)
            .ThenByDescending(teamId => bestEntryByTeamId[teamId].Score)
            .ThenByDescending(teamId => bestEntryByTeamId[teamId].HighGame);

        foreach (var teamId in rankedTeamIds)
        {
            foreach (var bowlerId in membersByTeamId.GetValueOrDefault(teamId, []).Where(missingBowlerIds.Contains))
            {
                places[bowlerId] = nextPlace;
            }

            nextPlace++;
        }

        // Every bowler still missing a Place has no non-forfeited roster at all - they share one
        // common last place, tied, not individually ranked against each other.
        foreach (var bowlerId in missingBowlerIds.Where(id => !places.ContainsKey(id)))
        {
            places[bowlerId] = nextPlace;
        }

        return places;
    }
}