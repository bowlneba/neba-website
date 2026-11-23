using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Tournaments;

/// <summary>A bowler who won a tournament.</summary>
internal readonly record struct TournamentWinner(BowlerId BowlerId, Name Name);

/// <summary>
/// One team that may bowl together as defending champions of a tournament.
/// </summary>
/// <param name="SourceTournamentId">The tournament the team won.</param>
/// <param name="SourceTournamentName">The name of the tournament the team won.</param>
/// <param name="SeasonDescription">The season of the tournament the team won.</param>
/// <param name="SourceTruncated">True when the tournament the team won was truncated.</param>
/// <param name="Undefended">True when the team won an earlier tournament and never got to defend, because a later tournament in the chain was truncated.</param>
/// <param name="Champions">The bowlers on the team.</param>
/// <param name="ConsecutiveTitles">How many tournaments in a row, ending with the one the team won, the same bowlers won. Skipped years do not break the run, because a tournament's previous tournament is the one it follows.</param>
internal sealed record DefendingTeam(
    TournamentId SourceTournamentId,
    string SourceTournamentName,
    string SeasonDescription,
    bool SourceTruncated,
    bool Undefended,
    IReadOnlyList<TournamentWinner> Champions,
    int ConsecutiveTitles = 1);

/// <summary>
/// Loads the defending teams of the given tournaments. A tournament's first defending team is the winners of its
/// previous tournament. While the tournament just reached is truncated, the winners of its own previous tournament
/// are added too. A previous tournament must end before the next one starts, so the walk cannot loop.
/// </summary>
internal sealed class DefendingTeamsReader(AppDbContext appDbContext)
{
    // The chain is short in practice (a truncated tournament in a row is rare). The cap only guards against
    // a loop that bad data could cause. A title run counts at most this many tournaments before the source, so a
    // count tops out at MaxChainLength + 1.
    internal const int MaxChainLength = 10;

    private sealed record SourceRow(
        TournamentId Id,
        string Name,
        string SeasonDescription,
        TournamentStatus Status,
        TournamentId? PreviousTournamentId);

    public async Task<IReadOnlyDictionary<TournamentId, IReadOnlyList<DefendingTeam>>> LoadAsync(
        IReadOnlyCollection<TournamentId> tournamentIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tournamentIds);

        var result = new Dictionary<TournamentId, IReadOnlyList<DefendingTeam>>();

        if (tournamentIds.Count == 0)
        {
            return result;
        }

        var ownerIds = tournamentIds.ToArray();

        // The tournament just reached, per owner (the tournament whose defenders we want).
        var frontier = (await appDbContext.Tournaments
                .AsNoTracking()
                .Where(tournament => ownerIds.Contains(tournament.Id) && tournament.PreviousTournamentId != null)
                .Select(tournament => new { tournament.Id, tournament.PreviousTournamentId })
                .ToListAsync(cancellationToken))
            .ToDictionary(row => row.Id, row => row.PreviousTournamentId);

        var sourcesByOwner = new Dictionary<TournamentId, List<SourceRow>>();

        for (var depth = 0; frontier.Count > 0 && depth < MaxChainLength; depth++)
        {
            var sourceIds = frontier.Values.OfType<TournamentId>().Distinct().ToArray();

            var sources = (await appDbContext.Tournaments
                    .AsNoTracking()
                    .Where(tournament => sourceIds.Contains(tournament.Id))
                    .Select(tournament => new SourceRow(
                        tournament.Id,
                        tournament.Name,
                        tournament.Season.Description,
                        tournament.Status,
                        tournament.PreviousTournamentId))
                    .ToListAsync(cancellationToken))
                .ToDictionary(row => row.Id);

            var next = new Dictionary<TournamentId, TournamentId?>();

            foreach (var (ownerId, sourceId) in frontier)
            {
                if (sourceId is not { } id || !sources.TryGetValue(id, out var source))
                {
                    continue;
                }

                if (!sourcesByOwner.TryGetValue(ownerId, out var list))
                {
                    sourcesByOwner[ownerId] = list = [];
                }

                list.Add(source);

                // A truncated tournament leaves its own previous tournament's winners undefended.
                if (source.Status == TournamentStatus.Truncated && source.PreviousTournamentId is { } previousId)
                {
                    next[ownerId] = previousId;
                }
            }

            frontier = next;
        }

        var allSourceIds = sourcesByOwner.Values
            .SelectMany(list => list.Select(source => source.Id))
            .Distinct()
            .ToArray();

        var winners = await LoadWinnersAsync(allSourceIds, cancellationToken);

        var titleRuns = await LoadTitleRunsAsync(
            [.. sourcesByOwner.Values.SelectMany(list => list).DistinctBy(source => source.Id)],
            winners,
            cancellationToken);

        foreach (var (ownerId, sources) in sourcesByOwner)
        {
            result[ownerId] = [.. sources
                .Select((source, index) => (source, index))
                .Where(item => winners.ContainsKey(item.source.Id))
                .Select(item => new DefendingTeam(
                    item.source.Id,
                    item.source.Name,
                    item.source.SeasonDescription,
                    item.source.Status == TournamentStatus.Truncated,
                    item.index > 0,
                    winners[item.source.Id],
                    titleRuns.GetValueOrDefault(item.source.Id, 1)))];
        }

        return result;
    }

    // Counts, per source tournament, how many tournaments in a row the same bowlers won. Walks back through each
    // previous tournament and stops at the first one with other winners, no winners, or no previous tournament.
    private async Task<Dictionary<TournamentId, int>> LoadTitleRunsAsync(
        List<SourceRow> sources,
        Dictionary<TournamentId, IReadOnlyList<TournamentWinner>> sourceWinners,
        CancellationToken cancellationToken)
    {
        var runs = new Dictionary<TournamentId, int>();
        var frontier = new Dictionary<TournamentId, TournamentId>();

        foreach (var source in sources)
        {
            runs[source.Id] = 1;

            if (source.PreviousTournamentId is { } previousId && sourceWinners.ContainsKey(source.Id))
            {
                frontier[source.Id] = previousId;
            }
        }

        for (var depth = 0; frontier.Count > 0 && depth < MaxChainLength; depth++)
        {
            var earlierIds = frontier.Values.Distinct().ToArray();

            var earlierWinners = await LoadWinnersAsync(earlierIds, cancellationToken);

            var earlierPrevious = (await appDbContext.Tournaments
                    .AsNoTracking()
                    .Where(tournament => earlierIds.Contains(tournament.Id))
                    .Select(tournament => new { tournament.Id, tournament.PreviousTournamentId })
                    .ToListAsync(cancellationToken))
                .ToDictionary(row => row.Id, row => row.PreviousTournamentId);

            var next = new Dictionary<TournamentId, TournamentId>();

            foreach (var (sourceId, earlierId) in frontier)
            {
                if (!earlierWinners.TryGetValue(earlierId, out var earlier)
                    || !SameBowlers(sourceWinners[sourceId], earlier))
                {
                    continue;
                }

                runs[sourceId]++;

                if (earlierPrevious.GetValueOrDefault(earlierId) is { } earlierPreviousId)
                {
                    next[sourceId] = earlierPreviousId;
                }
            }

            frontier = next;
        }

        return runs;
    }

    private static bool SameBowlers(IReadOnlyList<TournamentWinner> first, IReadOnlyList<TournamentWinner> second)
        => first.Select(winner => winner.BowlerId).ToHashSet()
            .SetEquals(second.Select(winner => winner.BowlerId));

    // Place-1 bowlers from historical champions and recorded results. A title does not need to be eligible:
    // a truncated tournament's first place counts.
    private async Task<Dictionary<TournamentId, IReadOnlyList<TournamentWinner>>> LoadWinnersAsync(
        TournamentId[] tournamentIds,
        CancellationToken cancellationToken)
    {
        if (tournamentIds.Length == 0)
        {
            return [];
        }

        var historical = await appDbContext.HistoricalTournamentChampions
            .AsNoTracking()
            .Where(champion => tournamentIds.Contains(champion.Tournament.Id))
            .Select(champion => new { TournamentId = champion.Tournament.Id, BowlerId = champion.Bowler.Id, champion.Bowler.Name })
            .ToListAsync(cancellationToken);

        var recorded = await appDbContext.Tournaments
            .AsNoTracking()
            .Where(tournament => tournamentIds.Contains(tournament.Id))
            .SelectMany(tournament => tournament.Results
                .Where(result => result.Place == 1)
                .Select(result => new { TournamentId = tournament.Id, BowlerId = result.Bowler.Id, result.Bowler.Name }))
            .ToListAsync(cancellationToken);

        return historical.Concat(recorded)
            .GroupBy(row => row.TournamentId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<TournamentWinner>)[.. group.Select(row => new TournamentWinner(row.BowlerId, row.Name))]);
    }
}