using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.ListTournamentsInSeason;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.ListUpcomingTournaments;

/// <summary>
/// Builds the upcoming list from the per-season cached query, so a tournament edit that evicts a
/// season's cache entry also refreshes this list, and the date cut-off runs after the cache read
/// (a cached season never shows a tournament that has since ended).
/// </summary>
internal sealed class ListUpcomingTournamentsQueryHandler(
    AppDbContext appDbContext,
    IQueryHandler<ListTournamentsInSeasonQuery, IReadOnlyCollection<SeasonTournamentDto>> seasonTournamentsHandler,
    TimeProvider timeProvider)
    : IQueryHandler<ListUpcomingTournamentsQuery, IReadOnlyCollection<SeasonTournamentDto>>
{
    private readonly IQueryable<Season> _seasons = appDbContext.Seasons.AsNoTracking();

    public async Task<IReadOnlyCollection<SeasonTournamentDto>> HandleAsync(
        ListUpcomingTournamentsQuery query, CancellationToken cancellationToken)
    {
        // Back up one day so a tournament that ends today stays listed in every U.S. time zone;
        // the website applies the exact "has it ended" check in its own time zone.
        var cutoff = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime).AddDays(-1);

        var seasonIds = await _seasons
            .Where(season => season.EndDate >= cutoff)
            .OrderBy(season => season.StartDate)
            .Select(season => season.Id)
            .ToListAsync(cancellationToken);

        var upcoming = new List<SeasonTournamentDto>();

        foreach (var seasonId in seasonIds)
        {
            var tournaments = await seasonTournamentsHandler.HandleAsync(
                new ListTournamentsInSeasonQuery
                {
                    SeasonId = seasonId,
                    CallerIsAuthenticated = query.CallerIsAuthenticated,
                    CallerHasTournamentManagementPermission = query.CallerHasTournamentManagementPermission
                },
                cancellationToken);

            upcoming.AddRange(tournaments.Where(tournament => tournament.EndDate >= cutoff));
        }

        return [.. upcoming.OrderBy(tournament => tournament.StartDate)];
    }
}