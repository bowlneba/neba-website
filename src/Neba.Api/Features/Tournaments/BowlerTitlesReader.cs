using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Tournaments;

/// <summary>
/// Loads the NEBA titles of many bowlers at once, for the team eligibility rules. Reads the same two sources as
/// the bowler titles query: historical champions, and first places in title-eligible recorded tournaments.
/// </summary>
internal sealed class BowlerTitlesReader(AppDbContext appDbContext)
{
    public async Task<ILookup<BowlerId, NebaTitle>> LoadAsync(
        IReadOnlyCollection<BowlerId> bowlerIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bowlerIds);

        if (bowlerIds.Count == 0)
        {
            return Enumerable.Empty<(BowlerId, NebaTitle)>().ToLookup(row => row.Item1, row => row.Item2);
        }

        var ids = bowlerIds.ToArray();

        var historical = await appDbContext.HistoricalTournamentChampions
            .AsNoTracking()
            .Where(champion => ids.Contains(champion.Bowler.Id))
            .Select(champion => new { BowlerId = champion.Bowler.Id, champion.Tournament.StartDate, champion.Tournament.EndDate })
            .ToListAsync(cancellationToken);

        // Same filter as the bowler titles query: only title-eligible tournaments count.
        var recorded = await appDbContext.Tournaments
            .AsNoTracking()
            .Where(tournament => tournament.TitleEligible)
            .SelectMany(tournament => tournament.Results
                .Where(result => result.Place == 1 && ids.Contains(result.Bowler.Id))
                .Select(result => new { BowlerId = result.Bowler.Id, tournament.StartDate, tournament.EndDate }))
            .ToListAsync(cancellationToken);

        return historical.Concat(recorded)
            .ToLookup(row => row.BowlerId, row => new NebaTitle(row.StartDate, row.EndDate));
    }
}
