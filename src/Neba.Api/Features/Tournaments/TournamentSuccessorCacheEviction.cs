using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.Tournaments;

/// <summary>
/// Evicts the cached list and detail of tournaments that show another tournament's winners as defending champions.
/// A tournament's defending teams, and the count of titles in a row, sit inside the cached reads of the tournaments
/// after it, which are usually in a later season, so a change to a tournament's winners, status, name or season must
/// reach them.
/// </summary>
internal static class TournamentSuccessorCacheEviction
{
    // Same bound as the defending teams reader: the chain is short, and the cap only guards against bad data.
    private const int MaxChainLength = 10;

    /// <summary>A tournament whose cached reads show the evicting tournament's winners.</summary>
    internal readonly record struct Successor(TournamentId Id, SeasonId SeasonId);

    /// <summary>
    /// Finds every tournament that names <paramref name="tournamentId"/> as its previous tournament. A truncated
    /// tournament passes its own previous winners down and a run of titles counts back through every earlier
    /// tournament, so the walk continues through every successor.
    /// </summary>
    public static async Task<IReadOnlyList<Successor>> FindAsync(
        AppDbContext appDbContext, TournamentId tournamentId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(appDbContext);

        var found = new List<Successor>();
        var seen = new HashSet<TournamentId> { tournamentId };
        List<TournamentId?> frontier = [tournamentId];

        for (var depth = 0; frontier.Count > 0 && depth < MaxChainLength; depth++)
        {
            var successors = await appDbContext.Tournaments
                .AsNoTracking()
                .Where(tournament => frontier.Contains(tournament.PreviousTournamentId))
                .Select(tournament => new { tournament.Id, tournament.SeasonId })
                .ToListAsync(cancellationToken);

            var fresh = successors.Where(successor => seen.Add(successor.Id)).ToList();

            found.AddRange(fresh.Select(successor => new Successor(successor.Id, successor.SeasonId)));

            // Every successor passes its place on: a tournament two steps on shows a title count that spans
            // the whole run of titles.
            frontier = [.. fresh.Select(successor => (TournamentId?)successor.Id)];
        }

        return found;
    }

    /// <summary>Evicts the cached detail and season list of each successor.</summary>
    public static async Task EvictAsync(
        IFusionCache cache, IEnumerable<Successor> successors, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(successors);

        foreach (var successor in successors)
        {
            await cache.RemoveByTagAsync($"neba:tournaments:{successor.Id}", token: cancellationToken);
            await cache.RemoveByTagAsync($"neba:tournaments:{successor.SeasonId}", token: cancellationToken);
        }
    }

    /// <summary>Finds the successors of <paramref name="tournamentId"/> and evicts them.</summary>
    public static async Task EvictAsync(
        AppDbContext appDbContext, IFusionCache cache, TournamentId tournamentId, CancellationToken cancellationToken)
        => await EvictAsync(cache, await FindAsync(appDbContext, tournamentId, cancellationToken), cancellationToken);

    /// <summary>
    /// Evicts the cached season list of each given tournament. A season list tells which tournaments already have
    /// a next tournament, so it goes stale when a tournament starts or stops being another's previous tournament.
    /// </summary>
    public static async Task EvictPredecessorSeasonsAsync(
        AppDbContext appDbContext, IFusionCache cache, IEnumerable<TournamentId?> tournamentIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(appDbContext);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(tournamentIds);

        List<TournamentId?> ids = [.. tournamentIds.Where(id => id is not null).Distinct()];

        if (ids.Count == 0)
        {
            return;
        }

        var seasonIds = await appDbContext.Tournaments
            .AsNoTracking()
            .Where(tournament => ids.Contains(tournament.Id))
            .Select(tournament => tournament.SeasonId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var seasonId in seasonIds)
        {
            await cache.RemoveByTagAsync($"neba:tournaments:{seasonId}", token: cancellationToken);
        }
    }
}