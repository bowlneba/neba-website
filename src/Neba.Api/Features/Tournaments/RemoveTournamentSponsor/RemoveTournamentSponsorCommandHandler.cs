using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.Tournaments.RemoveTournamentSponsor;

internal sealed class RemoveTournamentSponsorCommandHandler(AppDbContext appDbContext, IFusionCache cache)
    : ICommandHandler<RemoveTournamentSponsorCommand, Deleted>
{
    public async Task<ErrorOr<Deleted>> HandleAsync(RemoveTournamentSponsorCommand command, CancellationToken cancellationToken)
    {
        var tournament = await appDbContext.Tournaments
            .Include(t => t.Sponsors)
            .SingleOrDefaultAsync(t => t.Id == command.TournamentId, cancellationToken);

        if (tournament is null)
        {
            return TournamentErrors.TournamentNotFound(command.TournamentId);
        }

        var result = tournament.RemoveSponsor(command.SponsorId);

        if (result.IsError)
        {
            return result.Errors;
        }

        await appDbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync($"neba:tournaments:{tournament.Id}", token: cancellationToken);
        await cache.RemoveByTagAsync($"neba:tournaments:{tournament.SeasonId}", token: cancellationToken);

        return Result.Deleted;
    }
}
