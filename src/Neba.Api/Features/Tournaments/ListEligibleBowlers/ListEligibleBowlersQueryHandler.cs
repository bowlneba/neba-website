using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.ListEligibleBowlers;

internal sealed class ListEligibleBowlersQueryHandler(AppDbContext appDbContext)
    : IQueryHandler<ListEligibleBowlersQuery, ErrorOr<EligibleBowlersDto>>
{
    // Far above any season's bowler count (about 1,000 members), so no qualifier is ever cut.
    private const int MaxBowlers = 5000;

    public async Task<ErrorOr<EligibleBowlersDto>> HandleAsync(
        ListEligibleBowlersQuery query, CancellationToken cancellationToken)
    {
        var invitational = await appDbContext.Tournaments
            .AsNoTracking()
            .Where(tournament => tournament.Id == query.Id)
            .Select(tournament => new { tournament.SeasonId, tournament.TournamentType, tournament.Status })
            .SingleOrDefaultAsync(cancellationToken);

        if (invitational is null)
        {
            return TournamentErrors.TournamentNotFound(query.Id);
        }

        // Compared in memory, after the SmartEnums load; not a SQL predicate.
        if (invitational.TournamentType != TournamentType.Invitational
            || invitational.Status != TournamentStatus.Scheduled)
        {
            return new EligibleBowlersDto { Requirements = null, Bowlers = [] };
        }

        var seasonId = invitational.SeasonId;
        var threshold = Tournament.InvitationalEligibilityThreshold;

        // Every result counts: results exist only on Completed or Truncated tournaments, and
        // (tournament, bowler) is unique, so Count() is the number of distinct tournaments.
        var eligibleBowlerIds = appDbContext.Tournaments
            .AsNoTracking()
            .Where(tournament => tournament.SeasonId == seasonId)
            .SelectMany(tournament => tournament.Results)
            .GroupBy(result => result.BowlerId)
            .Where(group => group.Count() >= threshold)
            .Select(group => group.Key);

        var rows = await appDbContext.Bowlers
            .AsNoTracking()
            .Where(bowler => eligibleBowlerIds.Contains(bowler.Id))
            .OrderBy(bowler => bowler.Id)
            .Take(MaxBowlers)
            .Select(bowler => new { bowler.Id, bowler.Name, bowler.UsbcId })
            .ToListAsync(cancellationToken);

        // Sorted here, not in SQL, so the order does not depend on the database collation.
        var bowlers = rows
            .OrderBy(row => row.Name.LastName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.Name.FirstName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.Id.Value)
            .Select(row => new EligibleBowlerSummaryDto
            {
                Name = row.Name.ToDisplayName(),
                UsbcId = row.UsbcId
            })
            .ToList();

        return new EligibleBowlersDto
        {
            Requirements = new EligibilityRequirementsDto { MinTournamentsCount = threshold },
            Bowlers = bowlers
        };
    }
}
