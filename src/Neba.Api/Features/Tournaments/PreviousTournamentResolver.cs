using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Tournaments;

/// <summary>
/// Loads the facts about a tournament's previous tournament, or confirms there is none to load.
/// Shared by the create and edit command handlers, like <see cref="TournamentSeasonAndPatternResolver"/>.
/// </summary>
internal static class PreviousTournamentResolver
{
    public static async Task<ErrorOr<PreviousTournamentFacts?>> ResolveAsync(
        AppDbContext appDbContext,
        TournamentId? previousTournamentId,
        CancellationToken cancellationToken)
    {
        if (previousTournamentId is not { } id)
        {
            return (PreviousTournamentFacts?)null;
        }

        var facts = await appDbContext.Tournaments
            .AsNoTracking()
            .Where(tournament => tournament.Id == id)
            .Select(tournament => new { tournament.Id, tournament.TournamentType, tournament.EndDate })
            .SingleOrDefaultAsync(cancellationToken);

        return facts is null
            ? TournamentErrors.PreviousTournamentNotFound(id)
            : (PreviousTournamentFacts?)new PreviousTournamentFacts(facts.Id, facts.TournamentType, facts.EndDate);
    }
}
