using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.Tournaments.TruncateTournament;

internal sealed class TruncateTournamentCommandHandler(AppDbContext appDbContext, IFusionCache cache)
    : ICommandHandler<TruncateTournamentCommand, Success>
{
    public async Task<ErrorOr<Success>> HandleAsync(TruncateTournamentCommand command, CancellationToken cancellationToken)
    {
        var tournament = await appDbContext.Tournaments
            .SingleOrDefaultAsync(t => t.Id == command.TournamentId, cancellationToken);

        if (tournament is null)
        {
            return TournamentErrors.TournamentNotFound(command.TournamentId);
        }

        var truncateResult = tournament.TruncateTournament();
        if (truncateResult.IsError)
        {
            return truncateResult.Errors;
        }

        await appDbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync($"neba:tournaments:{tournament.Id}", token: cancellationToken);
        await cache.RemoveByTagAsync($"neba:tournaments:{tournament.SeasonId}", token: cancellationToken);

        return Result.Success;
    }
}