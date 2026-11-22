using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Tournaments;

/// <summary>
/// Loads the facts about a tournament's previous tournament, or confirms there is none to load.
/// Rejects a tournament another tournament already follows. Shared by the create and edit command handlers, like <see cref="TournamentSeasonAndPatternResolver"/>.
/// </summary>
internal static class PreviousTournamentResolver
{
    public static async Task<ErrorOr<PreviousTournamentFacts?>> ResolveAsync(
        AppDbContext appDbContext,
        TournamentId? previousTournamentId,
        CancellationToken cancellationToken,
        TournamentId? selfId = null)
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

        if (facts is null)
        {
            return TournamentErrors.PreviousTournamentNotFound(id);
        }

        // A tournament can be the previous tournament of one other tournament only.
        var usedByAnother = await appDbContext.Tournaments
            .AsNoTracking()
            .AnyAsync(tournament => tournament.PreviousTournamentId == id && tournament.Id != selfId, cancellationToken);

        return usedByAnother
            ? TournamentErrors.PreviousTournamentAlreadyUsed(id)
            : (PreviousTournamentFacts?)new PreviousTournamentFacts(facts.Id, facts.TournamentType, facts.EndDate);
    }
}